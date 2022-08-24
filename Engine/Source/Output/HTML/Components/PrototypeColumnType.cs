﻿/*
 * Enum: CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeColumnType
 * ____________________________________________________________________________
 *
 * A prototype's parameter column type.  Note that the prototype CSS classes are directly mapped to these
 * names.
 *
 * ModifierQualifier - For C-style prototypes, a separate column for modifiers and qualifiers.  For Pascal-style
 *							  prototypes, any modifiers that appear before the name.
 * Type - The parameter type.  For C-style prototypes this will only be the last word.  For Pascal-style
 *			  prototypes this will be the entire symbol.
 * TypeNameSeparator - For Pascal-style prototypes, the symbol separating the name from the type.
 * Symbols - Symbols between names and types that should be formatted in a separate column, such as *
 *				   and &.
 * Name - The parameter name.
 * DefaultValueSeparator - If present, the symbol for assigning a default value like = or :=.
 * DefaultValue - The default value.
 * PropertyValueSeparator - If present, the symbol for assigning a value to a property like = or :.
 * PropertyValue - The property value, such as could appear in Java annotations.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public enum PrototypeColumnType : byte
		{
		ModifierQualifier, Type, TypeNameSeparator,
		Symbols, Name,
		DefaultValueSeparator, DefaultValue,
		PropertyValueSeparator, PropertyValue
		}
	}
