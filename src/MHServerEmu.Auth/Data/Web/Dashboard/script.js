const apiUtil = {
	handleReadyStateChange(xhr, callback) {
		if (xhr.readyState == 4 && xhr.status == 200) {
			const response = JSON.parse(xhr.responseText);
			callback(response);
		}
	},
	
	get(path, callback) {
		const url = window.location.origin + path;

		const xhr = new XMLHttpRequest();
		xhr.open("GET", url, true);
		xhr.onreadystatechange = () => this.handleReadyStateChange(xhr, callback);
		xhr.send();
	},

	post(path, data, callback) {
		const url = window.location.origin + path;
		const json = JSON.stringify(data);

		const xhr = new XMLHttpRequest();
		xhr.open("POST", url, true);
		xhr.onreadystatechange = () => this.handleReadyStateChange(xhr, callback);
		xhr.setRequestHeader("Content-Type", "application/json");
		xhr.send(json);
	},
}

const htmlUtil = {
	createAndAppendChild(parent, tagName, text = "") {
		var child = document.createElement(tagName);

		if (text != "") {
			const textNode = document.createTextNode(text);
			child.appendChild(textNode);
		}

		parent.appendChild(child);
		return child;
	},

	createAndAppendTable(parent, tableData) {
		const table = this.createAndAppendChild(parent, "table");

		for (let i = 0; i < tableData.length; i++) {
			const rowData = tableData[i];
			const row = this.createAndAppendChild(table, "tr");

			for (let j = 0; j < rowData.length; j++) {
				const cellData = rowData[j];
				this.createAndAppendChild(row, "td", cellData);
			}
		}
	}
}

const stringUtil = {
	bigIntToHexString(value, upper = true) {
		let str = BigInt(value).toString(16);

		if (upper)
			str = str.toUpperCase();

		return str;
	}
}

const tabManager = {
	currentTabId: "",

	initialize(tabs) {
		for (let i = 0; i < tabs.length; i++) {
			const tab = tabs[i];
			document.getElementById(tab.tabName + "-tab-button").onclick = () => this.openTab(tab);
			tab.initialize();
		}

		this.openTab(null);
	},

	openTab(tab) {
		const tabId = tab != null ? `${tab.tabName}-tab` : "";
		
		const tabs = document.getElementsByClassName("tab-content");
		for (let i = 0; i < tabs.length; i++) {
			tabs[i].style.display = "none";
		}
		
		if (tabId == "") {
			return;
		}
		else if (tabId == this.currentTabId) {
			this.currentTabId = "";
			return;
		}

		this.currentTabId = tabId;
		document.getElementById(tabId).style.display = "block";
	},
}

const serverStatusTab = {
	tabName: "server-status",

	initialize() {
		document.getElementById("server-status-button").onclick = () => this.requestData();
	},

	requestData() {
		apiUtil.get("/ServerStatus", (data) => this.onDataReceived(data));
	},

	onDataReceived(data) {
		const serverStatusContainer = document.getElementById("server-status-container");
		serverStatusContainer.innerHTML = "";

		htmlUtil.createAndAppendChild(serverStatusContainer, "pre", data.Text);
	}
}

const metricsTab = {
	tabName: "metrics",

	initialize() {
		document.getElementById("metrics-button").onclick = () => this.requestData();
		document.getElementById("metrics-game-select").onchange = () => this.onGameMetricSelectChanged();
	},

	requestData() {
		apiUtil.get("/Metrics/Performance", (data) => this.onDataReceived(data));
	},

	onDataReceived(data) {
		this.updateReportMetadata(data);
		this.updateMemoryMetrics(data.Memory);
		this.updateGameMetrics(data.Games);
	},

	onGameMetricSelectChanged() {
		const select = document.getElementById("metrics-game-select");
		const metric = select.options[select.selectedIndex].value;
		this.selectGameMetric(metric);
	},

	updateReportMetadata(data) {
		const metadataContainer = document.getElementById("metrics-report-metadata");
		metadataContainer.innerHTML = "";

		htmlUtil.createAndAppendChild(metadataContainer, "p", `Report 0x${stringUtil.bigIntToHexString(data.Id)}`);
	},

	updateMemoryMetrics(data) {
		const memoryContainer = document.getElementById("metrics-memory-container");
		memoryContainer.innerHTML = "";
		
		const memoryList = htmlUtil.createAndAppendChild(memoryContainer, "ul");
		htmlUtil.createAndAppendChild(memoryList, "li", `GCIndex: ${data.GCIndex}`)
		htmlUtil.createAndAppendChild(memoryList, "li", `GCCounts: Gen0=${data.GCCountGen0}, Gen1=${data.GCCountGen1}, Gen2=${data.GCCountGen2}`)
		htmlUtil.createAndAppendChild(memoryList, "li", `HeapSizeBytes: ${data.HeapSizeBytes.toLocaleString()} / ${data.TotalCommittedBytes.toLocaleString()}`)
		htmlUtil.createAndAppendChild(memoryList, "li", `PauseTimePercentage: ${data.PauseTimePercentage}%`)
		htmlUtil.createAndAppendChild(memoryList, "li", `PauseDuration: ${this.formatTracker(data.PauseDuration)}`)
	},

	updateGameMetrics(data) {
		const gameMetricSelect = document.getElementById("metrics-game-select");
		let prevSelectedIndex = gameMetricSelect.selectedIndex;
		gameMetricSelect.innerHTML = "";

		const gameMetricContainer = document.getElementById("metrics-game-container");
		gameMetricContainer.innerHTML = "";

		const dataMap = new Map();

		// Bucket data by metric
		for (const key in data) {
			const entry = data[key];

			for (const metric in entry) {
				const value = entry[metric];

				if (dataMap.has(metric) == false) {
					dataMap.set(metric, [["GameId", "Avg", "Mdn", "Last", "Min", "Max"]]);
				}

				dataMap.get(metric).push([
					`0x${stringUtil.bigIntToHexString(key)}`,
					value.Average.toFixed(2),
					value.Median.toFixed(2),
					value.Last.toFixed(2),
					value.Min.toFixed(2),
					value.Max.toFixed(2)]);
			}
		}
		
		// Populate html with bucketed data
		dataMap.forEach((value, key) => {
			const selectOption = htmlUtil.createAndAppendChild(gameMetricSelect, "option", key);
			selectOption.value = key;

			const subcontainer = htmlUtil.createAndAppendChild(gameMetricContainer, "div");
			subcontainer.id = `${key}-subcontainer`;
			subcontainer.className = "game-metric-subcontainer";

			htmlUtil.createAndAppendTable(subcontainer, value);
		});

		// Reselect the previously selected metric
		if (dataMap.size > 0) {
			if (prevSelectedIndex == -1) {
				prevSelectedIndex = 0;
			}

			gameMetricSelect.selectedIndex = prevSelectedIndex;
			this.onGameMetricSelectChanged();
		}
	},

	selectGameMetric(metric) {
		const metricSubcontainerId = metric != "" ? `${metric}-subcontainer` : "";
		
		const subcontainers = document.getElementsByClassName("game-metric-subcontainer");
		for (let i = 0; i < subcontainers.length; i++) {
			subcontainers[i].style.display = "none";
		}
		
		if (metricSubcontainerId == "") {
			return;
		}

		document.getElementById(metricSubcontainerId).style.display = "block";
	},

	formatTracker(tracker) {
		return `avg=${tracker.Average.toFixed(2)}, mdn=${tracker.Median.toFixed(2)}, last=${tracker.Last.toFixed(2)}, min=${tracker.Min.toFixed(2)}, max=${tracker.Max.toFixed(2)}`;
	}
}

const regionReportTab = {
	tabName: "region-report",

	initialize() {
		document.getElementById("region-report-button").onclick = () => this.requestData();	
	},

	requestData() {
		apiUtil.get("/RegionReport", (data) => this.onDataReceived(data));
	},

	onDataReceived(data) {
		const list = document.getElementById("region-report-list");
		list.innerHTML = "";

		let gameId = 0;
		let gameSublist = null;

		for (let i = 0; i < data.Regions.length; i++) {
			const region = data.Regions[i];
			const regionText = `[0x${stringUtil.bigIntToHexString(region.RegionId)}] ${region.Name} (${region.DifficultyTier}) - ${region.Uptime}`;

			if (gameId != region.GameId) {
				const gameText = `Game [0x${stringUtil.bigIntToHexString(region.GameId)}]`;
				htmlUtil.createAndAppendChild(list, "li", gameText);
				gameSublist = htmlUtil.createAndAppendChild(list, "ul");
				gameId = region.GameId;
			}

			htmlUtil.createAndAppendChild(gameSublist, "li", regionText);
		}
	}
}

const createAccountTab = {
	tabName: "create-account",

	initialize() {
		document.getElementById("create-account-submit").onclick = () => this.createAccount();
	},

	createAccount() {
		const email = document.getElementById("create-account-email");
		const playerName = document.getElementById("create-account-player-name");
		const password = document.getElementById("create-account-password");
		const confirmPassword = document.getElementById("create-account-confirm-password");

		confirmPassword.setCustomValidity("");

		if (email.reportValidity() == false || playerName.reportValidity() == false || password.reportValidity() == false) {
			return;
		}

		if (password.value != confirmPassword.value) {
			confirmPassword.setCustomValidity("Your passwords do not match.");
			confirmPassword.reportValidity();
			return;
		}

		const accountData = {
			Email: email.value,
			PlayerName: playerName.value,
			Password: password.value
		};

		apiUtil.post("/AccountManagement/Create", accountData, (result) => this.onCreateAccountResult(result));
	},

	onCreateAccountResult(result) {
		window.alert(result.Text);
	}
}

tabManager.initialize([
	serverStatusTab,
	metricsTab,
	regionReportTab,
	createAccountTab
]);
