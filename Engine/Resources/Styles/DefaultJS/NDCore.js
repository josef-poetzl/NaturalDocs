﻿/*
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


// Location Info Members

	$LocationInfo_SimpleIdentifier = 0;
	$LocationInfo_Folder = 1;
	$LocationInfo_Type = 2;
	$LocationInfo_PrefixRegexString = 3;
	$LocationInfo_PrefixRegexObject = 4;

// Location Info Type values

	$LocationInfoType_File = 0
	$LocationInfoType_LanguageSpecificHierarchy = 1;
	$LocationInfoType_LanguageAgnosticHierarchy = 2;



/* Class: NDCore
	_____________________________________________________________________________

    Various helper functions to be used throughout the other scripts.
*/
var NDCore = new function ()
	{


	// Group: JavaScript Loading Functions
	// ________________________________________________________________________


	/* Function: LoadJavaScript
		Dynamically adds a script tag to the document head which loads the JavaScript file at the path.  If desired you
		can give it an ID so you can remove the tag with <RemoveScriptElement()> later.
	*/
	this.LoadJavaScript = function (path, id)
		{
		var script = document.createElement("script");
		script.src = path;
		script.type = "text/javascript";

		if (id != undefined)
			{  script.id = id;  }

		document.getElementsByTagName("head")[0].appendChild(script);

		// This intentionally does not return the script element.  In IE8 the data file's callback may fire before this function
		// returns, thus whatever was going to store the script element may be undefined when other code doesn't expect
		// it to be.  Instead you need to use the ID parameter which will always work.
		};


	/* Function: RemoveScriptElement
		Removes a script element from the document using the passed ID.
	*/
	this.RemoveScriptElement = function (id)
		{
		var script = document.getElementById(id);
		script.parentNode.removeChild(script);
		};



	// Group: Positioning Functions
	// ________________________________________________________________________


	/* Function: GetFullOffsets
		Returns an object with the cumulative offsetTop and offsetLeft of the passed DOM element, going all the
		way up to the page body.
	*/
	this.GetFullOffsets = function (element)
		{
		var result = { offsetTop: element.offsetTop, offsetLeft: element.offsetLeft };
		element = element.offsetParent;

		while (element != undefined && element.nodeName != "BODY")
			{
			result.offsetTop += element.offsetTop;
			result.offsetLeft += element.offsetLeft;
			element = element.offsetParent;
			}

		return result;
		};



	// Group: Hash and Path Functions
	// ________________________________________________________________________


	/* Function: NormalizeHash

		Returns a normalized version of the passed hash string.

		- The leading hash symbol will be removed if present.
		- URL encoded characters will be decoded.
		- Undefined, empty strings, and empty hashes will all be converted to an empty string so they compare as equal.

	*/
	this.NormalizeHash = function (hashString)
		{
		if (hashString == undefined)
			{  return "";  }

		if (hashString.charAt(0) == "#")
			{  hashString = hashString.substr(1);  }

		hashString = decodeURI(hashString);
		return hashString;
		};


	/* Function: AddQueryParams

		Adds query parameters to the passed URL, inserting them in between the file and the hash anchor.

		Example:

			URL - "file:///myfile.html#Anchor"
			OueryParams - "Var=Value"
			Result - "file:///myfile.html?Var=Value#Anchor"
	*/
	this.AddQueryParams = function (url, queryParams)
		{
		var hashIndex = url.indexOf("#");

		if (hashIndex == -1)
			{  return (url + "?" + queryParams);  }
		else
			{  return (url.slice(0, hashIndex) + "?" + queryParams + url.slice(hashIndex));  }
		};


	/* Function: GetQueryParam
		
		Returns the value of a query parameter if it appears in the current URL, or undefined if not.

		This function works similarly to URLSearchParams, but is our own implementation because IE11 and some
		other browsers don't support it.
	*/
	this.GetQueryParam = function (param)
		{
		var queryString = window.location.search;

		if (queryString == undefined)
			{  return undefined;  }

		var paramIndex = queryString.indexOf("?" + param + "=");

		if (paramIndex == -1)
			{  paramIndex = queryString.indexOf(";" + param + "=");  }

		if (paramIndex == -1)
			{  return undefined;  }

		var valueIndex = paramIndex + param.length + 2;

		var endValueIndex = queryString.indexOf(";", valueIndex);

		if (endValueIndex == -1)
			{  return queryString.slice(valueIndex);  }
		else
			{  return queryString.slice(valueIndex, endValueIndex);  }
		};



	// Group: Prototype Functions
	// ________________________________________________________________________


	/* Function: ChangePrototypeToNarrowForm
		Changes the passed NDPrototype element to use the narrow form.  The prototype *must* be in the wide form.
	*/
	this.ChangePrototypeToNarrowForm = function (prototype)
		{
		prototype.classList.remove("WideForm");
		prototype.classList.add("NarrowForm");

		var divs = prototype.getElementsByTagName("div");

		for (var i = 0; i < divs.length; i++)
			{
			if (divs[i].dataset.narrowgridarea)
				{  divs[i].style.gridArea = divs[i].dataset.narrowgridarea;  }
			}
		};


	/* Function: ChangePrototypeToWideForm
		Changes the passed NDPrototype element to use the wide form.  The prototype *must* be in the narrow form.
	*/
	this.ChangePrototypeToWideForm = function (prototype)
		{
		prototype.classList.remove("NarrowForm");
		prototype.classList.add("WideForm");

		var divs = prototype.getElementsByTagName("div");

		for (var i = 0; i < divs.length; i++)
			{
			if (divs[i].dataset.widegridarea)
				{  divs[i].style.gridArea = divs[i].dataset.widegridarea;  }
			}
		};



	// Group: Style Functions
	// ________________________________________________________________________


	/* Function: GetComputedStyle
		Returns the computed CSS style for the passed element in a browser-neutral way.  It first tries the element's
		inline styles in case it overrides them, and if not, retrieves the results created from the style sheets.  Returns
		undefined if it's not set.
	*/
	this.GetComputedStyle = function (element, style)
		{
		// First try inline.
		var result = element.style[style];

		// All tested browsers return an empty string if it's not set.
		if (result != "")
			{  return result;  }

		// Now try computed.  This was tested to work in Firefox 3.6+, Chrome 12+, and Opera 11.6.
		// IE works starting with 9 but 6-8 are out of luck.
		// Online docs say Safari only supports document.defaultView.getComputedStyle(), but Safari 5 handles this fine.
		if (window.getComputedStyle)
			{
			return window.getComputedStyle(element, "")[style];
			}

		else
			{
			return undefined;
			}
		};

	/* Function: GetComputedPixelWidth
		Similar to <GetComputedStyle()> except that it returns the property as an integer representing the pixel width.
		If the CSS property is in any format other than "#px" it will return zero, so it can't decode "#em", "#ex", etc.
	*/
	this.GetComputedPixelWidth = function (element, style)
		{
		var result = this.GetComputedStyle(element, style);

		if (this.pxRegex.test(result))
			{  return parseInt(RegExp.$1, 10);  }
		else
			{  return 0;  }
		};



	// Group: Variables
	// ________________________________________________________________________


	/* var: pxRegex
		A regular expression that can interpret "12px" styles, leaving the integer in the RegExp.$1 variable.
	*/
	this.pxRegex = /^([0-9]+)px$/i;

	};



// Section: Extension Functions
// ____________________________________________________________________________


/* Function: String.StartsWith
	Returns whether the string starts with or is equal to the passed string.
*/
String.prototype.StartsWith = function (other)
	{
	if (other === undefined)
		{  return false;  }

	return (this.length >= other.length && this.substr(0, other.length) == other);
	};


/* Function: String.EntityDecode
	Returns the string with entity chars like &amp; replaced with their original characters.  Only substitutes characters
	found in <CodeClear.NaturalDocs.Engine.StringExtensions.EntityEncode()>.
*/
String.prototype.EntityDecode = function ()
	{
	// DEPENDENCY: Must update this whenever StringExtensions.EntityEncode() is changed.

	var output = this;

	// Using string constants instead of regular expressions doesn't allow a global substitution.
	output = output.replace(/&lt;/g, "<");
	output = output.replace(/&gt;/g, ">");
	output = output.replace(/&quot;/g, "\"");
	output = output.replace(/&amp;/g, "&");

	return output;
	};


/*
	Class: NDLocation
	___________________________________________________________________________

	A class encompassing all the information decoded from a Natural Docs hash path.

*/
function NDLocation (hashString)
	{

	// Group: Private Functions
	// ________________________________________________________________________


	/* Private Function: Constructor
		You do not need to call this function.  Simply call "new NDLocation(hashString)" and this will be called automatically.
	 */
	this.Constructor = function (hashString)
		{

		//
		// Normalize our hash string
		//

		this.hashString = NDCore.NormalizeHash(hashString);


		//
		// Split it up into path, member, and prefix
		//

		// The first colon separates the path prefix from the rest of the path, such as the one after File: or CSharpClass:.
		// This may not exist if this is a Home or invalid hash path.
		var prefixSeparator = this.hashString.indexOf(':');
		var memberSeparator = -1;

		if (prefixSeparator == -1)
			{
			this.prefix = undefined;
			this.path = undefined;
			this.member = undefined;
			}
		else
			{
			this.prefix = this.hashString.substr(0, prefixSeparator);

			// The second colon, which separates the path from the member.  This may not exist if the path just goes
			// to a file or class and not one of its members.
			memberSeparator = this.hashString.indexOf(':', prefixSeparator + 1);

			if (memberSeparator == -1)
				{
				// Remember path includes the prefix
				this.path = this.hashString;
				this.member = undefined;
				}
			else
				{
				this.path = this.hashString.substr(0, memberSeparator);
				this.member = this.hashString.substr(memberSeparator + 1);

				if (this.member == "")
					{  this.member = undefined;  }
				}
			}


		//
		// Determine the type from the prefix
		//

		var foundPrefix = false;
		var locationInfo = undefined;
		var prefixParam = undefined;

		if (this.hashString == "")
			{
			this.type = "Home";
			foundPrefix = true;
			}

		else if (NDFramePage != undefined &&
				  NDFramePage.locationInfo != undefined)
			{
			for (var i = 0; i < NDFramePage.locationInfo.length; i++)
				{
				var matchResult = this.prefix.match( NDFramePage.locationInfo[i][$LocationInfo_PrefixRegexObject] );

				if (matchResult)
					{
					locationInfo = NDFramePage.locationInfo[i];
					this.type = locationInfo[$LocationInfo_SimpleIdentifier];
					prefixParam = matchResult[1];

					foundPrefix = true;
					break;
					}
				}
			}

		// No matches or NDFramePage.locationInfo isn't loaded yet
		if (!foundPrefix)
			{
			this.type = "Home";
			}


		//
		// Determine URLs for contentPage, summaryFile, and summaryTTFile
		//

		if (this.type == "Home" || locationInfo == undefined)
			{
			this.contentPage = "other/home.html";
			this.summaryFile = undefined;
			this.summaryTTFile = undefined;
			}
		else
			{
			var path = locationInfo[$LocationInfo_Folder];

			if (locationInfo[$LocationInfo_Type] == $LocationInfoType_File && prefixParam != undefined)
				{  path += prefixParam;  }  // files2
			else if (locationInfo[$LocationInfo_Type] == $LocationInfoType_LanguageSpecificHierarchy)
				{  path += '/' + prefixParam;  }  // classes/CSharp

			path += '/' + this.path.substr(prefixSeparator + 1);

			if (locationInfo[$LocationInfo_Type] == $LocationInfoType_File)
				{
				var lastSeparator = path.lastIndexOf('/');
				var folder = path.substr(0, lastSeparator + 1);
				var file = path.substr(lastSeparator + 1);

				// Need to use replace() with a regex because older versions of Safari don't have replaceAll()
				file = file.replace(/\./g, '-');

				path = folder + file;
				}
			else // hierarchy
				{
				path = path.replace(/\./g, '/');
				}

			this.contentPage = path + ".html";
			this.summaryFile = path + "-Summary.js";
			this.summaryTTFile = path + "-SummaryToolTips.js";

			if (this.member != undefined)
				{  this.contentPage += '#' + this.member;  }
			}
		};



	// Group: Variables
	// ___________________________________________________________________________


	/*
		var: hashString
		The full normalized hash string.


		var: type

		A string representing the type of location this is.

		Possible Values:

			"Home" - The home page.  For now if there's an invalid hash path the location will be set the home page.  In the future
						  there may be a separate 404-type page.
			"File" - A source file.
			"Class" - A class.
			other strings - Other strings that don't appear in this list since it's expandable by entries in <NDFramePage.locationInfo>.
								 Code should be able to handle strings besides the above.


		var: path

		If <hashString> can be split into a path and member, this will be the path.  This includes the prefix like File:.

		Examples:

			File - "File:Folder/Folder/Source.cs"
			Class - "CSharpClass:Namespace.Namespace.Class"


		var: member

		If <hashString> can be split into a path and member and a member is specified, this will be the member.  Some
		hashes will only include a <path> and not a member.

		Examples:

			File - "Class.Class.Member" in "File:Folder/Folder/Source.cs:Class.Class.Member"
			Class - "Member" in "CSharpClass:Namespace.Namespace.Class:Member"


		var: prefix

		If <hashString> contains a path, this will be just the prefix.  It does not include the colon.

		Examples:

			File - "File" in "File:Folder/Folder/Source.cs"
			Class - "CSharpClass" in "CSharpClass:Namespace.Namespace.Class"


		var: contentPage
		The URL to the content page.

		var: summaryFile
		The URL to the summary data file, or undefined if none.

		var: summaryTTFile
		The URL to the summary tooltips data file, or undefined if none.
	*/


	// Call the constructor now that all the members are prepared.
	this.Constructor(hashString);

	};
