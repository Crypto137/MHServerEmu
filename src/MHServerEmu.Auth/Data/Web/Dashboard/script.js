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

	initialize() {
		document.getElementById("server-status-tab-button").onclick = function() { tabManager.openTab('server-status-tab'); }
		document.getElementById("metrics-tab-button").onclick = function() { tabManager.openTab('metrics-tab'); }
		document.getElementById("region-report-tab-button").onclick = function() { tabManager.openTab('region-report-tab'); }
		document.getElementById("create-account-tab-button").onclick = function() { tabManager.openTab('create-account-tab'); }

		tabManager.openTab("");
	},

	openTab(tabId) {
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
	}
}

const regionManager = {
	initialize() {
		document.getElementById("region-report-button").onclick = this.requestData;	
	},

	requestData() {
		console.log("request");
		apiUtil.get("/RegionReport", function(data) { regionManager.onDataReceived(data); })
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

const accountManager = {
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

		apiUtil.post("/AccountManagement/Create", accountData, function(result) { accountManager.onCreateAccountResult(result); });
	},

	onCreateAccountResult(result) {
		window.alert(result.Text);
	}
}

tabManager.initialize();
regionManager.initialize();
accountManager.initialize();
