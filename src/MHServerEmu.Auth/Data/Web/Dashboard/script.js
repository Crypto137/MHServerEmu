const tabManager = {
	currentTabId: "",

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

const apiUtil = {
	postJson(path, data) {
		const url = window.location.origin + path + "?outputFormat=json";	// Remove outputFormat when we deprecate the old web frontend
		const json = JSON.stringify(data);

		var xhr = new XMLHttpRequest();
		xhr.open("POST", url, true);
		xhr.setRequestHeader("Content-Type", "application/json");
		xhr.onreadystatechange = function () {
			if (xhr.readyState == 4 && xhr.status == 200) {
				const response = JSON.parse(xhr.responseText);
				window.alert(response.Text);
			}
		};

		xhr.send(json);
	}
}

const accountManager = {
	createAccount(evt) {
		const email = document.getElementById("email");
		const playerName = document.getElementById("player-name");
		const password = document.getElementById("password");
		const confirmPassword = document.getElementById("confirm-password");

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

		apiUtil.postJson("/AccountManagement/Create", accountData);
	}
}

tabManager.openTab("");