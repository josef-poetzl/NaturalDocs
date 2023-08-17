/*
	Include in output:

	This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
	Natural Docs is licensed under version 3 of the GNU Affero General Public
	License (AGPL).  Refer to License.txt or www.naturaldocs.org for the
	complete details.

	This file may be distributed with documentation files generated by Natural Docs.
	Such documentation is not covered by Natural Docs' copyright and licensing,
	and may have its own copyright and distribution terms as decided by its author.

*/

"use strict";


// Theme Members

	$Theme_Name = 0;
	$Theme_ID = 1;

// Theme History

	$ThemeHistory_Count = 10;
	$ThemeHistory_Key = "NDThemes.UserSelectedThemeHistory";

// Keycodes

	$KeyCode_Escape = 27;




/* Class: NDThemes
	_____________________________________________________________________________

    A class to manage and apply the available themes.


	Topic: Auto-Theme

		Auto-themes are user-selectable themes where the actual effective theme depends on what the operating
		system setting is.  So "Auto Light/Dark" will switch between the Light and Dark themes based on what the
		system is set to, or Light if that feature isn't supported.


	Topic: Theme ID

		A theme ID is just a string representing a theme which only uses the characters A-Z.  This makes it safe to
		include in CSS identifiers and other places.  So the light theme's ID is just "Light".

		For <auto-themes>, the theme ID is a string in the format "Auto:[Light Theme ID]/[Dark Theme ID]".  So
		for an auto-theme that switches between Light and Black depending on the operating system setting, the ID
		would be "Auto:Light/Black".

		This is the difference between <userSelectedThemeID> and <effectiveThemeID>.  For <auto-themes> the
		user selected theme ID would be "Auto:Light/Black" but the effective theme ID would be "Light" or "Black".
		For regular themes the IDs would be the same.


	Topic: Theme History

		A history of which themes the user has selected is stored in the web browser so that they can be applied
		automatically the next time the documentation is opened.  This is also necessary just to have the theme
		persist when opening new windows or tabs.

		It is stored as a most-recently-used list of <theme IDs> the user has selected.  Themes which were applied
		automatically or which were forced by the documentation's author are not added.  This allows the list to apply
		across documentation sets: if a user selects Dark for documentation that has Light, Dark, and Black, and then
		selects Red for documentation that only has Red and Blue, the list will contain both Dark and Red so that
		whichever one is available in the current documentation can be applied.

		It is stored in window.localStorage with $ThemeHistory_Key as the key.  The value is a comma-separated
		list of <theme IDs>.

*/
var NDThemes = new function ()
	{

	// Group: Functions
	// ________________________________________________________________________


	/* Function: SetCurrentTheme

		Sets the passed <theme ID> as the current user selected one.  If it's an <auto-theme> the effective theme
		ID may be different.  You may pass undefined for none.

		Parameters:

			themeID - The <theme ID> to apply, which may be an <auto-theme>.

			userSelected - Set to true to indicate the end user actively chose this theme as opposed to it being set
								 automatically.

		Events:

			- <NDEffectiveThemeChange> will be dispatched if this results in a change to <effectiveThemeID>.

	*/
	this.SetCurrentTheme = function (themeID, userSelected)
		{
		var oldUserSelectedThemeID = this.userSelectedThemeID;
		var oldEffectiveThemeID = this.effectiveThemeID;

		var newUserSelectedThemeID = themeID;
		var newEffectiveThemeID = themeID;


		// Apply auto-themes

//		if (newUserSelectedThemeID.startsWith("Auto:"))
//			{
//			var systemTheme = this.GetSystemTheme();
//
//			if (systemTheme == "Light")
//				{  this.effectiveTheme = "Light";  }
//			else
//				{
//				if (theme == "Auto-Light/Dark")
//					{  this.effectiveTheme = "Dark";  }
//				else
//					{  this.effectiveTheme = "Black";  }
//				}
//			}


		// Update the theme state

		this.userSelectedThemeID = newUserSelectedThemeID;
		this.effectiveThemeID = newEffectiveThemeID;


		// Save the user selection

		if (userSelected)
			{
			if (this.userSelectedThemeHistory == undefined)
				{  this.LoadUserSelectedThemeHistory();  }

			this.AddToUserSelectedThemeHistory(newUserSelectedThemeID);

			this.SaveUserSelectedThemeHistory();
			}


		// Dispach a NDEffectiveThemeChange event if necessary

		if (newEffectiveThemeID != oldEffectiveThemeID)
			{
			document.dispatchEvent(
				new CustomEvent("NDEffectiveThemeChange", {
					detail: {
						oldUserSelectedThemeID: oldUserSelectedThemeID,
						oldEffectiveThemeID: oldEffectiveThemeID,

						newUserSelectedThemeID: newUserSelectedThemeID,
						newEffectiveThemeID: newEffectiveThemeID,

						userSelected: userSelected
						}
					})
				);
			}
		};


	/* Function: SetAvailableThemes

		Sets the list of themes the documentation supports.  The parameter is an array of themes, and each theme entry
		is itself an array, the first value being its display name and the second value its <theme ID>.

		Calling this function will cause a <NDAvailableThemesChange> event to be dispatched.

		If there is no user-selected theme, or the user-selected theme does not exist in the list of available themes, this
		function will change the theme to one of the available ones.  It will first attempt to find one in the <theme history>,
		but if it can't it will just use the first one on the list.

		Thus calling this function could also cause a <NDEffectiveThemeChange> event to be dispatched.

		Example:

			--- Code ---
			NDThemes.SetAvailableThemes([
			   [ "Light Theme", "Light" ],
			   [ "Dark Theme", "Dark" ],
			   [ "Black Theme", "Black" ]
			]);
			--------------
	*/
	this.SetAvailableThemes = function (themes)
		{
		// Set the available themes

		this.availableThemes = themes;
		document.dispatchEvent( new Event("NDAvailableThemesChange") );


		// Make sure the current theme appears in the available themes

		if (this.availableThemes == undefined)
			{  this.SetCurrentThemeID(undefined, false);  }
		else
			{
			var currentThemeIsInAvailableThemes = false;

			if (this.userSelectedThemeID != undefined)
				{
				for (var i = 0; i < this.availableThemes.length; i++)
					{
					if (this.userSelectedThemeID == this.availableThemes[i][$Theme_ID])
						{
						currentThemeIsInAvailableThemes = true;
						break;
						}
					}
				}


			// If the current theme isn't in the available themes, choose one from the history

			if (!currentThemeIsInAvailableThemes)
				{
				if (this.userSelectedThemeHistory == undefined)
					{  this.LoadUserSelectedThemeHistory();  }

				var newThemeID;

				if (this.userSelectedThemeHistory != undefined)
					{
					for (var ui = 0; ui < this.userSelectedThemeHistory.length; ui++)
						{
						for (var ai = 0; ai < this.availableThemes.length; ai++)
							{
							if (this.availableThemes[ai][$Theme_ID] == this.userSelectedThemeHistory[ui])
								{
								newThemeID = this.userSelectedThemeHistory[ui];
								break;
								}
							}

						if (newThemeID != undefined)
							{  break;  }
						}
					}


				// If there wasn't one in the history, just use the first

				if (newThemeID == undefined)
					{  newThemeID = this.availableThemes[0][$Theme_ID];  }

				this.SetCurrentTheme(newThemeID, false);
				}
			}
		};


	/* Function: ForceTheme
		Sets the passed <theme ID> as the current user selected one and disables all other themes.  This is equivalent
		to calling <SetCurrentTheme()> and then <SetAvailableThemes()> with only this one available.

		If it's an <auto-theme> the effective theme ID may be different.  You may pass undefined for none.
	*/
	this.ForceTheme = function (themeID)
		{
		this.SetCurrentTheme(themeID, false);

		if (themeID == undefined)
			{  this.SetAvailableThemes(undefined);  }
		else
			{  this.SetAvailableThemes( [[themeID, themeID]] );  }
		};


	/* Function: DisableThemes
		Disables all themes.  This is eqivalent to calling <ForceTheme()> with undefined.
	*/
	this.DisableThemes = function ()
		{
		this.ForceTheme(undefined);
		};



	// Group: Support Functions
	// ________________________________________________________________________


	/* Function: LoadUserSelectedThemeHistory
		Loads <userSelectedThemeHistory> from the web browser's local storage.  It will be set to undefined if there
		are none.
	*/
	this.LoadUserSelectedThemeHistory = function ()
		{
		var joinedString = window.localStorage.getItem($ThemeHistory_Key);

		if (joinedString == null)
			{  this.userSelectedThemeHistory = undefined;  }
		else
			{  this.userSelectedThemeHistory = joinedString.split(",");  }
		};


	/* Function: SaveUserSelectedThemeHistory
		Saves <userSelectedThemeIDHistry> into the web browser's local storage.
	*/
	this.SaveUserSelectedThemeHistory = function ()
		{
		if (this.userSelectedThemeHistory == undefined)
			{  window.localStorage.removeItem($ThemeHistory_Key);  }
		else
			{
			var joinedString = this.userSelectedThemeHistory.join(",");
			window.localStorage.setItem($ThemeHistory_Key, joinedString);
			}
		};


	/* Function: AddToUserSelectedThemeHistory
		Adds an entry to <userSelectedThemeIDHistry>.
	*/
	this.AddToUserSelectedThemeHistory = function (themeID)
		{
		if (this.userSelectedThemeHistory == undefined)
			{  this.userSelectedThemeHistory = [ themeID ];  }
		else
			{
			var index = this.userSelectedThemeHistory.indexOf(themeID);

			// Remove the existing entry if it exists and isn't already at the start
			if (index > 0)
				{  this.userSelectedThemeHistory.splice(index, 1);  }

			// Add the entry at start if it's not already there
			if (index != 0)
				{  this.userSelectedThemeHistory.unshift(themeID);  }

			// Trim the list
			if (this.userSelectedThemeHistory.length > $ThemeHistory_Count)
				{  this.userSelectedThemeHistory.splice($ThemeHistory_Count);  }
			}
		};


	/* Function: GetSystemTheme
		Returns the operating system theme as the string "Light" or "Dark".  It defaults to "Light" if this isn't supported.
	*/
	this.GetSystemTheme = function ()
		{
		if (window.matchMedia &&
			window.matchMedia('(prefers-color-scheme: dark)').matches)
			{  return "Dark";  }
		else
			{  return "Light";  }
		};


	/* Function: AddSystemThemeChangeWatcher
		Sets a function to be called whenever the system theme changes.  The function will receive one parameter, the
		string "Light" or "Dark".
	*/
	this.AddSystemThemeChangeWatcher = function (changeWatcher)
		{
		if (window.matchMedia)
			{
			window.matchMedia('(prefers-color-scheme: dark)').addEventListener(
				'change',
				function (event)
					{
					var theme = event.matches ? "Dark" : "Light";
					changeWatcher(theme);
					}
				);
			}
		};



	// Group: Custom DOM Events
	// ________________________________________________________________________


	/* Event: NDEffectiveThemeChange

		This event is dispatched from the DOM Document object whenever the <effectiveThemeID> changes.

		Detail Properties:

			The event will have a "detail" property which is an object with the following values:

			oldUserSelectedThemeID - The previous value of <userSelectedThemeID>.
			oldEffectiveThemeID - The previous value of <effectiveThemeID>.

			newUserSelectedThemeID - The new value of <userSelectedThemeID>.
			newEffectiveThemeID - The new value of <effectiveThemeID>.

			userSelected - Whether the theme was actively chosen by the user as opposed to being set automatically.
	*/

	/* Event: NDAvailableThemesChange
		This event is dispatched from the DOM Document object whenever <availableThemes> changes.
	*/



	// Group: Variables
	// ________________________________________________________________________


	/* var: availableThemes

		An array of all the themes the documentation supports.  Each theme entry is itself an array, with the first value
		being its display name and the second value its <theme ID>.  The array will be undefined if none have been set.

		Example:

			--- Code ---
			[
			   [ "Light Theme", "Light" ],
			   [ "Dark Theme", "Dark" ],
			   [ "Black Theme", "Black" ]
			]
			-------------
		*/
	// this.availableThemes = undefined;


	/* var: userSelectedThemeID

		The <theme ID> of the user-selected theme, which could include <auto-themes>.  It will be undefined if one
		hasn't been set.

		When applying themes you always use <effectiveThemeID> instead as that translates auto-theme IDs into the
		one which should be applied.
	*/
	// this.userSelectedThemeID = undefined;


	/* var: userSelectedThemeHistory
		An array of <theme IDs> chosen by the user in a most-recently-used order.  Will be undefined if there are none
		or it hasn't been loaded yet.
	*/
	// this.userSelectedThemeHistory = undefined;


	/* var: effectiveThemeID
		The <theme ID> of the effective theme.  If <userSelectedThemeID> is an <auto-theme> such as "Auto:Light/Dark",
		this will be the theme ID which should actually be applied, such as "Light" or "Dark".  For non-auto-themes it will be
		the same as <userSelectedThemeID>.  It will be undefined if a theme hasn't been set.
	*/
	// this.effectiveThemeID = undefined;

	};



/* _____________________________________________________________________________

	Class: NDThemeSwitcher
	_____________________________________________________________________________

    A class to manage the HTML theme switcher, which allows the end user to choose their own theme at runtime.

*/
var NDThemeSwitcher = new function ()
	{

	// Group: Functions
	// ________________________________________________________________________


	/* Function: Start

		Sets up the theme switcher.  It expects there to be an empty HTML element with ID NDThemeSwitcher
		in the document.
	 */
	this.Start = function ()
		{

		// Create event handlers

		this.switcherClickEventHandler = NDThemeSwitcher.OnSwitcherClick.bind(NDThemeSwitcher);
		this.keyDownEventHandler = NDThemeSwitcher.OnKeyDown.bind(NDThemeSwitcher);


		// Prepare the switcher HTML

		this.domSwitcher = document.getElementById("NDThemeSwitcher");

		var domSwitcherLink = document.createElement("a");
		domSwitcherLink.addEventListener("click", this.switcherClickEventHandler);

		this.domSwitcher.appendChild(domSwitcherLink);


		// Prepare the pop-up menu holder HTML

		this.domMenu = document.createElement("div");
		this.domMenu.id = "NDThemeSwitcherMenu";
		this.domMenu.style.display = "none";
		this.domMenu.style.position = "fixed";

		document.body.appendChild(this.domMenu);
		};


	/* Function: IsNeeded
		Returns whether the theme switcher is necessary.  This will only return true if there are multiple themes
		defined in <NDThemes>.
	*/
	this.IsNeeded = function ()
		{
		return (NDThemes.availableThemes != undefined &&
				   NDThemes.availableThemes.length > 1);
		};


	/* Function: UpdateVisibility

		Creates or hides the theme switcher HTML elements depending on the results of <IsNeeded()>.  It returns
		whether the visibility changed.

		This should be called once to create it while setting up the page and again whenever the list of themes in
		<NDThemes> changes.
	*/
	this.UpdateVisibility = function ()
		{
		var themeSwitcher = document.getElementById("NDThemeSwitcher");

		var wasVisible = (themeSwitcher.style.display == "block");
		var shouldBeVisible = this.IsNeeded();

		if (!wasVisible && shouldBeVisible)
			{
			themeSwitcher.style.display = "block";
			return true;
			}
		else if (wasVisible && !shouldBeVisible)
			{
			themeSwitcher.style.display = "none";
			return true;
			}
		else
			{  return false;  }
		};



	// Group: Menu Functions
	// ________________________________________________________________________


	/* Function: OpenMenu
		Creates the pop-up menu, positions it, and makes it visible.
	*/
	this.OpenMenu = function ()
		{
		if (!this.MenuIsOpen())
			{
			this.BuildMenu();

			this.domMenu.style.visibility = "hidden";
			this.domMenu.style.display = "block";
			this.PositionMenu();
			this.domMenu.style.visibility = "visible";

			this.domSwitcher.classList.add("Active");

			window.addEventListener("keydown", this.keyDownEventHandler);
			}
		};


	/* Function: CloseMenu
		Closes the pop-up menu if it was visible.
	*/
	this.CloseMenu = function ()
		{
		if (this.MenuIsOpen())
			{
			this.domMenu.style.display = "none";
			this.domSwitcher.classList.remove("Active");

			window.removeEventListener("keydown", this.keyDownEventHandler);
			}
		};


	/* Function: MenuIsOpen
	*/
	this.MenuIsOpen = function ()
		{
		return (this.domMenu != undefined && this.domMenu.style.display == "block");
		};



	// Group: Menu Support Functions
	// ________________________________________________________________________


	/* Function: BuildMenu
		Creates the HTML pop-up menu from <NDThemes.availableThemes> and applies it to <domMenu>.  It does not
		affect its visibility or position.
	*/
	this.BuildMenu = function ()
		{
		var html = "";

		if (NDThemes.availableThemes != undefined)
			{
			for (var i = 0; i < NDThemes.availableThemes.length; i++)
				{
				var theme = NDThemes.availableThemes[i];

				html += "<a class=\"TSEntry TSEntry_" + theme[$Theme_ID] + "Theme\"";

				if (theme[$Theme_ID] == NDThemes.userSelectedThemeID)
					{  html += " id=\"TSSelectedEntry\"";  }

				html += " href=\"javascript:NDThemeSwitcher.OnMenuEntryClick('" + theme[$Theme_ID] + "');\">" +
					"<div class=\"TSEntryIcon\"></div>" +
					"<div class=\"TSEntryName\">" + theme[$Theme_Name] + "</div>" +
				"</a>";
				}
			}

		this.domMenu.innerHTML = html;
		};


	/* Function: PositionMenu
		Moves the pop-up menu into position relative to the button.
	*/
	this.PositionMenu = function ()
		{
		// First position it under the switcher

		var x = this.domSwitcher.offsetLeft;
		var y = this.domSwitcher.offsetTop + this.domSwitcher.offsetHeight + 5;


		// Now shift it over left enough so that the icons line up

		var entryIcons = this.domMenu.getElementsByClassName("TSEntryIcon");

		if (entryIcons != undefined && entryIcons.length >= 1)
			{
			var entryIcon = entryIcons[0];

			// offsetLeft is the icon offset relative to the parent menu, clientLeft is the menu's border width
			x -= entryIcon.offsetLeft + this.domMenu.clientLeft;
			}


		// Apply the position

		this.domMenu.style.left = x + "px";
		this.domMenu.style.top = y + "px";
		};



	// Group: Event Handlers
	// ________________________________________________________________________


	/* Function: OnSwitcherClick
	*/
	this.OnSwitcherClick = function (event)
		{
		if (this.MenuIsOpen())
			{  this.CloseMenu();  }
		else
			{  this.OpenMenu();  }

		event.preventDefault();
		};


	/* Function: OnMenuEntryClick
	*/
	this.OnMenuEntryClick = function (themeID)
		{
		NDThemes.SetCurrentTheme(themeID, true);
		this.CloseMenu();
		};


	/* Function: OnKeyDown
	*/
	this.OnKeyDown = function (event)
		{
		if (event.keyCode == $KeyCode_Escape)
			{
			if (this.MenuIsOpen())
				{
				this.CloseMenu();
				event.preventDefault();
				}
			}
		};


	/* Function: OnUpdateLayout
	*/
	this.OnUpdateLayout = function ()
		{
		// Check for undefined because this may be called before Start().
		if (this.domMenu != undefined)
			{
			this.PositionMenu();
			}
		};



	// Group: Event Handler Variables
	// ________________________________________________________________________

	/* var: switcherClickEventHandler
		A bound function to call <OnSwitcherClick()> with NDThemeSwitcher always as "this".
	*/

	/* var: keyDownEventHandler
		A bound function to call <OnKeyDown()> with NDThemeSwitcher always as "this".
	*/


	// Group: Variables
	// ________________________________________________________________________


	/* var: domSwitcher
		The DOM element of the theme switcher.
	*/
	// var domSwitcher = undefined;

	/* var: domMenu
		The DOM element of the pop-up theme menu.
	*/
	// var domMenu = undefined;

	};