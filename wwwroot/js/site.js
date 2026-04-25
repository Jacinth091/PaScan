(function () {
	var menuToggle = document.querySelector("[data-menu-toggle]");
	var menuPanel = document.querySelector("[data-menu-panel]");

	if (!menuToggle || !menuPanel) {
		return;
	}

	var desktopMinWidth = 834;

	function closeMenu() {
		menuPanel.classList.remove("is-open");
		menuToggle.setAttribute("aria-expanded", "false");
	}

	function openMenu() {
		menuPanel.classList.add("is-open");
		menuToggle.setAttribute("aria-expanded", "true");
	}

	menuToggle.addEventListener("click", function () {
		if (menuPanel.classList.contains("is-open")) {
			closeMenu();
			return;
		}

		openMenu();
	});

	document.addEventListener("click", function (event) {
		var target = event.target;
		if (menuPanel.contains(target) || menuToggle.contains(target)) {
			return;
		}

		closeMenu();
	});

	window.addEventListener("resize", function () {
		if (window.innerWidth >= desktopMinWidth) {
			closeMenu();
		}
	});
})();
