const apiUtil = {
	handleReadyStateChange(xhr, callback) {
		if (xhr.readyState == 4 && xhr.status == 200) {
			const response = JSON.parse(xhr.responseText);
			callback(response);
		}
	},
	
	get(path, callback) {
		const url = window.location.origin + path + "?outputFormat=json";	// Remove outputFormat when we deprecate the old web frontend

		var xhr = new XMLHttpRequest();
		xhr.open("GET", url, true);
		xhr.onreadystatechange = function () { apiUtil.handleReadyStateChange(xhr, callback) };
		xhr.send();
	},

	post(path, data, callback) {
		const url = window.location.origin + path + "?outputFormat=json";	// Remove outputFormat when we deprecate the old web frontend
		const json = JSON.stringify(data);

		var xhr = new XMLHttpRequest();
		xhr.open("POST", url, true);
		xhr.onreadystatechange = function () { apiUtil.handleReadyStateChange(xhr, callback) };
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
	}
}

const tabManager = {
	currentTabId: "",

	initialize(tabs) {
		var self = this;

		for (var i = 0; i < tabs.length; i++) {
			const tab = tabs[i];
			document.getElementById(tab.tabName + "-tab-button").onclick = function() { self.openTab(tab); }
			tab.initialize();
		}

		this.openTab("");
	},

	openTab(tab) {
		const tabId = tab.tabName + "-tab";
		
		var tabs = document.getElementsByClassName("tab-content");
		for (var i = 0; i < tabs.length; i++) {
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

	},
}

const metricsTab = {
	tabName: "metrics",

	initialize() {

	},
}

const regionReportTab = {
	tabName: "region-report",

	initialize() {
		document.getElementById("region-report-button").onclick = this.requestData;	
	},

	requestData() {
		console.log("request");
		apiUtil.get("/RegionReport", function(data) { regionReportTab.onDataReceived(data); })
	},

	onDataReceived(data) {
		var list = document.getElementById("region-report-list");
		list.innerHTML = "";

		var gameId = 0;
		var gameSublist = null;

		for (var i = 0; i < data.Regions.length; i++) {
			const region = data.Regions[i];
			const regionText = `[0x${region.RegionId}] ${region.Name} (${region.DifficultyTier}) - ${region.Uptime}`;

			if (gameId != region.GameId) {
				const gameText = `Game [0x${region.GameId}]`;
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
		document.getElementById("create-account-submit").onclick = this.createAccount;
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

		apiUtil.post("/AccountManagement/Create", accountData, function(result) { createAccountTab.onCreateAccountResult(result); });
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
