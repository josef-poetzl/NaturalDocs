﻿/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.SystemVerilog
 * ____________________________________________________________________________
 *
 * Full language support parser for SystemVerilog.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class SystemVerilog : Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: SystemVerilog
		 */
		public SystemVerilog (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
			}


		/* Function: ParsePrototype
		 * Converts a raw text prototype into a <ParsedPrototype>.
		 */
		override public ParsedPrototype ParsePrototype (string stringPrototype, int commentTypeID)
			{
			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			TokenIterator startOfPrototype = tokenizedPrototype.FirstToken;
			bool parsed = false;

			if (commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("module", language.ID) ||
				commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("systemverilog module", language.ID))
			    {
				parsed = TryToSkipModule(ref startOfPrototype, ParseMode.ParsePrototype);
			    }

			if (parsed)
				{  return new Prototypes.ParsedPrototypes.SystemVerilog(tokenizedPrototype, this.Language.ID, commentTypeID);  }
			else
			    {  return base.ParsePrototype(stringPrototype, commentTypeID);  }
			}


		/* Function: GetCodeElements
		 */
		override public List<Element> GetCodeElements (Tokenizer source)
			{
			List<Element> elements = new List<Element>();

			return elements;
			}


		/* Function: TryToFindBasicPrototype
		 * A temporary implementation to allow SystemVerilog to use the full language support functions to find prototypes following
		 * comments.  Natural Docs will still otherwise behave as if SystemVerilog has basic language support.
		 */
		override protected bool TryToFindBasicPrototype (Topic topic, LineIterator startCode, LineIterator endCode,
																			   out TokenIterator prototypeStart, out TokenIterator prototypeEnd)
			{
			if (topic.CommentTypeID != 0 &&
				( topic.CommentTypeID == EngineInstance.CommentTypes.IDFromKeyword("module", language.ID) ||
				  topic.CommentTypeID == EngineInstance.CommentTypes.IDFromKeyword("systemverilog module", language.ID) ))
				{
				TokenIterator startToken = startCode.FirstToken(LineBoundsMode.ExcludeWhitespace);
				TokenIterator endToken = endCode.FirstToken(LineBoundsMode.Everything);

				TokenIterator iterator = startToken;

				if (TryToSkipModule(ref iterator, ParseMode.IterateOnly) &&
					iterator <= endToken &&
					iterator.Tokenizer.ContainsTextBetween(topic.Title, true, startToken, iterator))
					{
					prototypeStart = startToken;
					prototypeEnd = iterator;
					return true;
					}
				else
					{
					prototypeStart = iterator;
					prototypeEnd = iterator;
					return false;
					}
				}

			// Fall back to the default implementation for everything else
			else
				{
				return base.TryToFindBasicPrototype(topic, startCode, endCode, out prototypeStart, out prototypeEnd);
				}
			}


		/* Function: SyntaxHighlight
		 */
		override public void SyntaxHighlight (Tokenizer source)
			{
			TokenIterator iterator = source.FirstToken;

			while (iterator.IsInBounds)
				{
				if (TryToSkipAttributes(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipComment(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipString(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipNumber(ref iterator, ParseMode.SyntaxHighlight))
					{
					}

				// Backslash identifiers.  Don't want them to mess up parsing other things since they can contain symbols.
				else if (iterator.Character == '\\' && TryToSkipUnqualifiedIdentifier(ref iterator, ParseMode.SyntaxHighlight))
					{
					}

				// Text.  Check for keywords.
				else if (iterator.FundamentalType == FundamentalType.Text ||
						   iterator.Character == '_' || iterator.Character == '$')
					{
					TokenIterator endOfIdentifier = iterator;

					do
						{  endOfIdentifier.Next();  }
					while (endOfIdentifier.FundamentalType == FundamentalType.Text ||
							  endOfIdentifier.Character == '_' || endOfIdentifier.Character == '$');

					// All SV keywords start with a lowercase letter or $
					if ((iterator.Character >= 'a' && iterator.Character <= 'z') || iterator.Character == '$')
						{
						string identifier = source.TextBetween(iterator, endOfIdentifier);

						if (Keywords.Contains(identifier))
							{  iterator.SetSyntaxHighlightingTypeByCharacters(SyntaxHighlightingType.Keyword, identifier.Length);  }
						}

					iterator = endOfIdentifier;
					}

				else
					{
					iterator.Next();
					}
				}
			}


		/* Function: IsBuiltInType
		 * Returns whether the type string is a built-in type such as "bit" as opposed to a user-defined type.
		 */
		override public bool IsBuiltInType (string type)
			{
			return BuiltInTypes.Contains(type);
			}



		// Group: Component Parsing Functions
		// __________________________________________________________________________


		/* Function: TryToSkipModule
		 *
		 * If the iterator is on a module, moves it past it and returns true.  If the mode is set to <ParseMode.CreateElements>
		 * it will add it to the list of <Elements>.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipModule (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly, List<Element> elements = null,
													   SymbolString scope = default(SymbolString))
			{
			#if DEBUG
			if (mode == ParseMode.CreateElements && elements == null)
				{  throw new Exception("Elements and scope must be set when using ParseMode.CreateElements().");  }
			#endif

			TokenIterator lookahead = iterator;


			// Extern

			bool hasExtern = false;
			TokenIterator externIterator = lookahead;

			if (lookahead.MatchesToken("extern"))
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead, mode);

				hasExtern = true;
				}


			// Attributes

			bool hasAttributes = false;

			if (TryToSkipAttributes(ref lookahead, mode))
				{
				TryToSkipWhitespace(ref lookahead, mode);

				// If the prototype has both extern and attributes, make extern its own section.
				if (mode == ParseMode.ParsePrototype && hasExtern)
					{  externIterator.PrototypeParsingType = PrototypeParsingType.EndOfPrototypeSection;  }

				hasAttributes = true;
				}


			// Keyword

			string keyword = null;

			if (lookahead.MatchesToken("module") ||
				lookahead.MatchesToken("macromodule"))
				{
				keyword = lookahead.String;

				// If the prototype has attributes, start a new section on the keyword.
				if (mode == ParseMode.ParsePrototype && hasAttributes)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.StartOfPrototypeSection;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead, mode);
				}
			else
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}


			// Lifetime

			if (lookahead.MatchesToken("static") ||
				lookahead.MatchesToken("automatic"))
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead, mode);
				}


			// Name

			string name = null;

			if (TryToSkipUnqualifiedIdentifier(ref lookahead, out name, mode))
				{
				TryToSkipWhitespace(ref lookahead, mode);
				}
			else
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}


			// Import

			if (TryToSkipImportDeclarations(ref lookahead, mode))
				{
				TryToSkipWhitespace(ref lookahead, mode);
				}


			// Parameter port list

			if (lookahead.MatchesAcrossTokens("#(") &&
				TryToSkipParameters(ref lookahead, mode))
				{
				TryToSkipWhitespace(ref lookahead, mode);
				}


			// Port declarations

			if (lookahead.Character == '(' &&
				TryToSkipParameters(ref lookahead, mode))
				{
				TryToSkipWhitespace(ref lookahead, mode);
				}


			GenericSkipUntilAfter(ref lookahead, ';');
			lookahead.Previous();  // Don't want to include semicolon
			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipStruct
		 *
		 * Tries to move the iterator past a struct or union.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipStruct (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{

			// Keyword

			if (!iterator.MatchesToken("struct") &&
				!iterator.MatchesToken("union"))
				{  return false;  }

			TokenIterator lookahead = iterator;

			string keyword = iterator.String;

			if (mode == ParseMode.ParsePrototype)
				{  lookahead.PrototypeParsingType = PrototypeParsingType.Type;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead, mode);


			// Modifiers

			if (lookahead.MatchesToken("tagged"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead, mode);
				}

			if (lookahead.MatchesToken("packed"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead, mode);

				if (lookahead.MatchesToken("signed") ||
					lookahead.MatchesToken("unsigned"))
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead, mode);
					}
				}


			// Body

			// The body is not optional, so fail if we didn't find it.
			if (lookahead.Character != '{')
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (mode == ParseMode.ParsePrototype)
				{  lookahead.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;  }

			GenericSkip(ref lookahead);

			if (mode == ParseMode.ParsePrototype)
				{
				TokenIterator closingBrace = lookahead;
				closingBrace.Previous();

				if (closingBrace.Character == '}')
					{  closingBrace.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;  }
				}

			// We're successful at this point, so update the iterator
			iterator = lookahead;


			// Dimensions

			// There can be additional dimensions after the body
			TryToSkipWhitespace(ref lookahead, mode);

			if (TryToSkipDimensions(ref lookahead, mode, PrototypeParsingType.TypeModifier))
				{
				iterator = lookahead;
				TryToSkipWhitespace(ref lookahead, mode);
				}


			// We've been updating iterator whenever we found part of the enum and continuing with lookahead
			// to see if we could extend it, so we need to reset anything between iterator and lookahead since that
			// part wasn't accepted.
			if (lookahead > iterator)
				{  ResetTokensBetween(iterator, lookahead, mode);  }

			return true;
			}


		/* Function: TryToSkipVirtualInterface
		 *
		 * Tries to move the iterator past a virtual interface type.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipVirtualInterface (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{

			// Keywords

			if (!iterator.MatchesToken("virtual"))
				{  return false;  }

			TokenIterator lookahead = iterator;

			if (mode == ParseMode.ParsePrototype)
				{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead, mode);

			if (lookahead.MatchesToken("interface"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead, mode);
				}


			// Type

			// The type is a simple identifier, no hierarchies
			if (!TryToSkipUnqualifiedIdentifier(ref lookahead, mode, PrototypeParsingType.Type))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			// We're successful if we've made it this far.  Everything else is optional.
			iterator = lookahead;

			TryToSkipWhitespace(ref lookahead, mode);


			// Parameters

			if (lookahead.MatchesAcrossTokens("#("))
				{
				if (mode == ParseMode.ParsePrototype)
					{
					lookahead.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;

					TokenIterator secondCharacter = lookahead;
					secondCharacter.Next();
					secondCharacter.PrototypeParsingType = PrototypeParsingType.OpeningExtensionSymbol;
					}

				GenericSkip(ref lookahead);

				if (mode == ParseMode.ParsePrototype)
					{
					TokenIterator endOfParameters = lookahead;
					endOfParameters.Previous();

					endOfParameters.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;
					}

				iterator = lookahead;

				TryToSkipWhitespace(ref lookahead, mode);
				}


			// Modport identifier

			if (lookahead.Character == '.')
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

				lookahead.Next();

				// This is a simple identifier, no hierarchies
				if (TryToSkipUnqualifiedIdentifier(ref lookahead, mode, PrototypeParsingType.TypeModifier))
					{
					iterator = lookahead;
					}
				}


			// We've been updating iterator whenever we found part of the enum and continuing with lookahead
			// to see if we could extend it, so we need to reset anything between iterator and lookahead since that
			// part wasn't accepted.
			if (lookahead > iterator)
				{  ResetTokensBetween(iterator, lookahead, mode);  }

			return true;
			}


		/* Function: TryToSkipParameters
		 *
		 * Tries to move the iterator past a comma-separated list of parameters in parentheses.  This supports both (
		 * and #( as the opening symbol.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipParameters (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{

			// Opening paren

			TokenIterator lookahead = iterator;

			if (lookahead.Character == '(')
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.StartOfParams;  }

				lookahead.Next();
				}
			else if (lookahead.MatchesAcrossTokens("#("))
				{
				lookahead.Next(2);

				if (mode == ParseMode.ParsePrototype)
					{  iterator.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.StartOfParams);  }
				}
			else
				{  return false;  }

			TryToSkipWhitespace(ref lookahead, mode);


			// Parameter list

			while (lookahead.IsInBounds && lookahead.Character != ')')
				{
				if (lookahead.Character == ',')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.ParamSeparator;  }

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead, mode);
					}
				else if (TryToSkipParameter(ref lookahead, mode))
					{
					TryToSkipWhitespace(ref lookahead, mode);
					}
				else
					{  break;  }
				}


			// Closing paren

			if (lookahead.Character == ')')
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.EndOfParams;  }

				lookahead.Next();
				iterator = lookahead;
				return true;
				}
			else
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}
			}


		/* Function: TryToSkipParameter
		 *
		 * Tries to move the iterator past a parameter, such as "string x".  The parameter ends at a comma or a closing
		 * parenthesis.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipParameter (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;


			// Parameter Keyword

			if (lookahead.MatchesToken("parameter") ||
				lookahead.MatchesToken("localparam"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.ParamKeyword;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead, mode);
				}


			// Input/Output

			if (lookahead.MatchesToken("input") ||
				lookahead.MatchesToken("output") ||
				lookahead.MatchesToken("inout") ||
				lookahead.MatchesToken("ref"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.InOut;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead, mode);
				}


			// Type vs. Name

			// Types can be implied, such as "bit paramA, paramB", so we have to check to see what comes after the first
			// identifier to see if it's a type or a parameter name.  If it's a comma (last thing before the next parameter)
			// an equals sign (last thing before the default value) or a closing parenthesis (end of last parameter) it's
			// definitely a name.

			// Also note that an implied type can also just be a dimension, such as "bit paramA, [8] paramB", so if it's not
			// on an identifier at all we can assume it has a type.

			bool hasType = true;
			TokenIterator startOfType = lookahead;

			if (!IsBuiltInType(lookahead.String) &&
				TryToSkipUnqualifiedIdentifier(ref lookahead, ParseMode.IterateOnly))
				{
				TryToSkipWhitespace(ref lookahead, ParseMode.IterateOnly);

				// Skip dimensions.  Both types and parameters can have them, such as "bit[8] paramA[2]", so their presence
				// doesn't tell us anything.
				if (TryToSkipDimensions(ref lookahead, ParseMode.IterateOnly))
					{  TryToSkipWhitespace(ref lookahead, ParseMode.IterateOnly);  }

				if (lookahead.Character == '=' ||
					lookahead.Character == ',' ||
					lookahead.Character == ')')
					{
					hasType = false;
					}

				// Reset back to start for parsing now that we know what it is
				lookahead = startOfType;
				}


			// Type

			// TryToSkipType covers signing and type dimensions
			if (hasType &&
				TryToSkipType(ref lookahead, mode) == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead, mode);


			// Name

			if (TryToSkipUnqualifiedIdentifier(ref lookahead, mode, PrototypeParsingType.Name) == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead, mode);


			// Param Dimensions.  Both types and parameters can have them, such as "bit[8] paramA[2]"

			if (TryToSkipDimensions(ref lookahead, mode, PrototypeParsingType.ParamModifier))
				{  TryToSkipWhitespace(ref lookahead, mode);  }


			// Default Value

			if (lookahead.Character == '=')
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.DefaultValueSeparator;  }

				lookahead.Next();
				TokenIterator startOfDefaultValue = lookahead;

				while (lookahead.IsInBounds &&
						 lookahead.Character != ',' &&
						 lookahead.Character != ')')
					{  GenericSkip(ref lookahead);  }

				if (mode == ParseMode.ParsePrototype)
					{  startOfDefaultValue.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.DefaultValue);  }
				}


			// End of Parameter

			if (lookahead.Character == ',' ||
				lookahead.Character == ')')
				{
				iterator = lookahead;
				return true;
				}
			else
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}
			}


		/* Function: TryToSkipEnum
		 *
		 * Tries to move the iterator past an enum.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipEnum (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{

			// Enum Keyword

			if (!iterator.MatchesToken("enum"))
				{  return false;  }

			TokenIterator lookahead = iterator;

			if (mode == ParseMode.ParsePrototype)
				{  lookahead.PrototypeParsingType = PrototypeParsingType.Type;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead, mode);


			// Type

			// Type is optional, so skip this if we're at the beginning of the body
			if (lookahead.Character != '{')
				{
				TokenIterator beginningOfType = lookahead;

				if (!TryToSkipType(ref lookahead, mode))
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				// We want the type to be "enum" and not something like "enum int", so change any type tokens that were
				// set by TryToSkipType().  We do want the signing and dimensions tokens it marked, so we can't use
				// IterateOnly instead.
				if (mode == ParseMode.ParsePrototype)
					{
					while (beginningOfType < lookahead)
						{
						if (beginningOfType.PrototypeParsingType == PrototypeParsingType.Type ||
							beginningOfType.PrototypeParsingType == PrototypeParsingType.TypeQualifier)
							{  beginningOfType.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

						beginningOfType.Next();
						}
					}

				TryToSkipWhitespace(ref lookahead, mode);
				}


			// Body

			// The body is not optional, so fail if we didn't find it.
			if (lookahead.Character != '{')
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (mode == ParseMode.ParsePrototype)
				{  lookahead.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;  }

			GenericSkip(ref lookahead);

			if (mode == ParseMode.ParsePrototype)
				{
				TokenIterator closingBrace = lookahead;
				closingBrace.Previous();

				if (closingBrace.Character == '}')
					{  closingBrace.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;  }
				}

			// We're successful at this point, so update the iterator
			iterator = lookahead;


			// Dimensions

			// There can be additional dimensions after the enum body, not just after the enum's type
			TryToSkipWhitespace(ref lookahead, mode);

			if (TryToSkipDimensions(ref lookahead, mode, PrototypeParsingType.TypeModifier))
				{
				iterator = lookahead;
				TryToSkipWhitespace(ref lookahead, mode);
				}


			// We've been updating iterator whenever we found part of the enum and continuing with lookahead
			// to see if we could extend it, so we need to reset anything between iterator and lookahead since that
			// part wasn't accepted.
			if (lookahead > iterator)
				{  ResetTokensBetween(iterator, lookahead, mode);  }

			return true;
			}


		/* Function: TryToSkipType
		 *
		 * Tries to move the iterator past a type, such as "string".  This can handle partially-implied types like "unsigned" and
		 * "[8:0]".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipType (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (TryToSkipEnum(ref iterator, mode) ||
				TryToSkipStruct(ref iterator, mode) ||
				TryToSkipVirtualInterface(ref iterator, mode))
				{  return true;  }

			TokenIterator lookahead = iterator;
			bool foundType = false;


			// Type Name and Sign

			// First check for a standalone "signed" or "unsigned" because implied types can be just that and we
			// don't want to mistake them for the type name.
			if (lookahead.MatchesToken("signed") ||
				lookahead.MatchesToken("unsigned"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

				lookahead.Next();
				iterator = lookahead;
				foundType = true;

				TryToSkipWhitespace(ref lookahead, mode);
				}

			// All other identifiers are treated as a type name
			else if (TryToSkipIdentifier(ref lookahead, mode, PrototypeParsingType.Type))
				{
				iterator = lookahead;
				foundType = true;

				TryToSkipWhitespace(ref lookahead, mode);

				// Now check again for "signed" or "unsigned" since they can follow some built-in types.
				if (lookahead.MatchesToken("signed") ||
					lookahead.MatchesToken("unsigned"))
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

					lookahead.Next();
					iterator = lookahead;
					// foundType is already true

					TryToSkipWhitespace(ref lookahead, mode);
					}
				}


			// Dimensions

			// Types can have dimensions such as "bit[8]", or implied types can be just a dimension like "[8]",
			// so not finding an identifier prior to this point doesn't mean we failed.

			if (TryToSkipDimensions(ref lookahead, mode, PrototypeParsingType.TypeModifier))
				{
				iterator = lookahead;
				foundType = true;

				TryToSkipWhitespace(ref lookahead, mode);
				}


			// We've been updating iterator whenever we found a type and continuing with lookahead to see if
			// we could extend it, so we need to reset anything between iterator and lookahead since that part
			// wasn't accepted into the type, even if foundType is true.  If foundType is false this will reset
			// everything.

			if (lookahead > iterator)
				{  ResetTokensBetween(iterator, lookahead, mode);  }

			return foundType;
			}


		/* Function: TryToSkipDimensions
		 *
		 * Tries to move the iterator past one or more consecutive dimensions, such as "[8:0][]".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.TypeModifier>
		 *			  or <PrototypeParsingType.ParamModifier>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipDimensions (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
															  PrototypeParsingType prototypeParsingType = PrototypeParsingType.TypeModifier)
			{
			if (!TryToSkipDimension(ref iterator, mode, prototypeParsingType))
				{  return false;  }

			TokenIterator lookahead = iterator;
			TryToSkipWhitespace(ref lookahead, mode);

			while (TryToSkipDimension(ref lookahead, mode, prototypeParsingType))
				{
				iterator = lookahead;
				TryToSkipWhitespace(ref lookahead, mode);
				}

			ResetTokensBetween(iterator, lookahead, mode);
			return true;
			}


		/* Function: TryToSkipDimension
		 *
		 * Tries to move the iterator past a single dimension, such as "[8:0]".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.TypeModifier>
		 *			  or <PrototypeParsingType.ParamModifier>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipDimension (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
															PrototypeParsingType prototypeParsingType = PrototypeParsingType.TypeModifier)
			{
			if (iterator.Character != '[')
				{  return false;  }

			TokenIterator lookahead = iterator;

			GenericSkip(ref lookahead);

			TokenIterator closingBracket = lookahead;
			closingBracket.Previous();

			if (closingBracket.Character != ']')
				{  return false;  }

			if (mode == ParseMode.ParsePrototype)
				{
				if (prototypeParsingType == PrototypeParsingType.TypeModifier ||
					prototypeParsingType == PrototypeParsingType.OpeningTypeModifier)
					{
					iterator.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;
					closingBracket.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;
					}
				else if (prototypeParsingType == PrototypeParsingType.ParamModifier ||
						   prototypeParsingType == PrototypeParsingType.OpeningParamModifier)
					{
					iterator.PrototypeParsingType = PrototypeParsingType.OpeningParamModifier;
					closingBracket.PrototypeParsingType = PrototypeParsingType.ClosingParamModifier;
					}
				else
					{
					iterator.PrototypeParsingType = prototypeParsingType;
					closingBracket.PrototypeParsingType = prototypeParsingType;
					}
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipAttributes
		 *
		 * If the iterator is on attributes, moves it past them and returns true.  This will skip multiple consecutive attributes
		 * blocks if they are only separated by whitespace.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAttributes (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (!TryToSkipAttributesBlock(ref iterator, mode))
				{  return false;  }

			TokenIterator lookahead = iterator;

			for (;;)
				{
				TryToSkipWhitespace(ref lookahead, mode);

				if (TryToSkipAttributesBlock(ref lookahead, mode))
					{
					iterator = lookahead;
					}
				else
					{
					ResetTokensBetween(iterator, lookahead, mode);
					break;
					}
				}

			return true;
			}


		/* Function: TryToSkipAttributesBlock
		 *
		 * If the iterator is on an attributes block, moves it past it and returns true.  This will only skip a single (* *) block.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAttributesBlock (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.MatchesAcrossTokens("(*") == false)
				{  return false;  }

			bool success = false;

			if (mode == ParseMode.ParsePrototype)
				{  iterator.SetPrototypeParsingTypeByCharacters(PrototypeParsingType.StartOfParams, 2);  }

			TokenIterator lookahead = iterator;
			lookahead.NextByCharacters(2);

			while (lookahead.IsInBounds)
				{
				if (lookahead.MatchesAcrossTokens("*)"))
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.SetPrototypeParsingTypeByCharacters(PrototypeParsingType.EndOfParams, 2);  }

					lookahead.NextByCharacters(2);

					success = true;
					break;
					}
				else if (lookahead.Character == '=')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.PropertyValueSeparator;  }

					lookahead.Next();
					}
				else if (lookahead.Character == ',')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.ParamSeparator;  }

					lookahead.Next();
					}
				else if (lookahead.Character == ')')
					{
					// If we found a ) before a *) then this isn't an attributes block.  It could be a pointer like (*pointer), so
					// break and fail.
					break;
					}
				else
					{
					// This will skip nested parentheses so we won't fail on the closing parenthesis of (* name = (value) *).
					GenericSkip(ref lookahead);
					}
				}

			if (!success)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (mode == ParseMode.SyntaxHighlight)
				{
				iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Metadata);
				}
			else if (mode == ParseMode.ParsePrototype)
				{
				// We marked StartOfParams, EndOfParams, ParamSeparator, and PropertyValueSeparator.
				// Find and mark tokens that should be Name and PropertyValue now.
				TokenIterator end = lookahead;
				lookahead = iterator;

				bool inValue = false;

				while (lookahead < end)
					{
					if (lookahead.PrototypeParsingType == PrototypeParsingType.PropertyValueSeparator)
						{  inValue = true;  }
					else if (lookahead.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
						{  inValue = false;  }
					else if (lookahead.PrototypeParsingType == PrototypeParsingType.Null &&
							   lookahead.FundamentalType != FundamentalType.Whitespace)
						{
						if (inValue)
							{  lookahead.PrototypeParsingType = PrototypeParsingType.PropertyValue;  }
						else
							{  lookahead.PrototypeParsingType = PrototypeParsingType.Name;  }
						}

					lookahead.Next();
					}
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipImportDeclarations
		 *
		 * If the iterator is on one or more import declarations, moves it past them and returns true.  This will skip multiple
		 * consecutive declarations if they are separated by whitespace.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipImportDeclarations (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (!TryToSkipImportDeclaration(ref iterator, mode))
				{  return false;  }

			TokenIterator lookahead = iterator;

			for (;;)
				{
				TryToSkipWhitespace(ref lookahead, mode);

				if (TryToSkipImportDeclaration(ref lookahead, mode))
					{
					iterator = lookahead;
					}
				else
					{
					ResetTokensBetween(iterator, lookahead, mode);
					break;
					}
				}

			return true;
			}


		/* Function: TryToSkipImportDeclaration
		 *
		 * If the iterator is on an import declaration, moves it past it and returns true.  This will only skip a single statement.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipImportDeclaration (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.MatchesToken("import") == false)
				{  return false;  }

			TokenIterator lookahead = iterator;

			if (mode == ParseMode.ParsePrototype)
				{  lookahead.PrototypeParsingType = PrototypeParsingType.StartOfPrototypeSection;  }

			lookahead.Next();

			if (mode == ParseMode.ParsePrototype)
				{  lookahead.PrototypeParsingType = PrototypeParsingType.StartOfParams;  }

			TryToSkipWhitespace(ref lookahead, mode);

			for (;;)
				{
				TokenIterator startOfName = lookahead;

				if (TryToSkipUnqualifiedIdentifier(ref lookahead, mode))
					{  }
				else
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				if (lookahead.MatchesAcrossTokens("::"))
					{
					lookahead.Next(2);
					}
				else
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				if (lookahead.Character == '*')
					{
					lookahead.Next();
					}
				else if (TryToSkipUnqualifiedIdentifier(ref lookahead, mode))
					{  }
				else
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				if (mode == ParseMode.ParsePrototype)
					{  startOfName.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.Name);  }

				TryToSkipWhitespace(ref lookahead, mode);

				if (lookahead.Character == ';')
					{
					// Let it stay part of the parameter instead of marking it the EndOfParams since we don't want it moving
					// to its own line on narrow prototypes.

					lookahead.Next();
					iterator = lookahead;
					return true;
					}
				else if (lookahead.Character == ',')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.ParamSeparator;  }

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead, mode);
					}
				else
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}
				}
			}


		/* Function: TryToSkipIdentifier
		 *
		 * Tries to move past and retrieve a qualified identifier, such as "X.Y.Z".  Use <TryToSkipUnqualifiedIdentifier()> if you only want
		 * to retrieve a single segment.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.Name>
		 *			  or <PrototypeParsingType.Type>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipIdentifier (ref TokenIterator iterator, out string identifier, ParseMode mode = ParseMode.IterateOnly,
														  PrototypeParsingType prototypeParsingType = PrototypeParsingType.Name)
			{
			TokenIterator start = iterator;

			if (TryToSkipIdentifier(ref iterator, mode))
				{
				identifier = start.TextBetween(iterator);
				return true;
				}
			else
				{
				identifier = null;
				return false;
				}
			}


		/* Function: TryToSkipIdentifier
		 *
		 * Tries to move the iterator past a qualified identifier, such as "X::Y::Z".  Use <TryToSkipUnqualifiedIdentifier()> if you only
		 * want to skip a single segment.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.Name>
		 *			  or <PrototypeParsingType.Type>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipIdentifier (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
														  PrototypeParsingType prototypeParsingType = PrototypeParsingType.Name)
			{
			TokenIterator startOfIdentifier = iterator;
			TokenIterator endOfIdentifier = iterator;
			TokenIterator endOfQualifier = iterator;

			// Can start with "$unit::".  Normal identifiers can't start with $ so we need to handle it separately.
			if (iterator.MatchesAcrossTokens("$unit"))
				{
				iterator.NextByCharacters(5);
				TryToSkipWhitespace(ref iterator, ParseMode.IterateOnly);

				if (iterator.MatchesAcrossTokens("::") == false)
					{
					iterator = startOfIdentifier;
					return false;
					}

				iterator.NextByCharacters(2);
				TryToSkipWhitespace(ref iterator, ParseMode.IterateOnly);

				endOfQualifier = iterator;
				}

			for (;;)
				{
				if (!TryToSkipUnqualifiedIdentifier(ref iterator, ParseMode.IterateOnly))
					{
					iterator = startOfIdentifier;
					return false;
					}

				endOfIdentifier = iterator;
				TryToSkipWhitespace(ref iterator, ParseMode.IterateOnly);

				if (iterator.MatchesAcrossTokens("#("))
					{
					GenericSkip(ref iterator);
					TryToSkipWhitespace(ref iterator, ParseMode.IterateOnly);
					}

				if (iterator.MatchesAcrossTokens("::"))
					{
					iterator.NextByCharacters(2);
					endOfQualifier = iterator;

					TryToSkipWhitespace(ref iterator, ParseMode.IterateOnly);
					}
				else
					{  break;  }
				}

			if (mode == ParseMode.ParsePrototype)
				{
				if (prototypeParsingType == PrototypeParsingType.Type)
					{
					if (startOfIdentifier < endOfQualifier)
						{  startOfIdentifier.SetPrototypeParsingTypeBetween(endOfQualifier, PrototypeParsingType.TypeQualifier);  }

					endOfQualifier.SetPrototypeParsingTypeBetween(endOfIdentifier, PrototypeParsingType.Type);
					}
				else
					{
					startOfIdentifier.SetPrototypeParsingTypeBetween(endOfIdentifier, prototypeParsingType);
					}
				}

			iterator = endOfIdentifier;
			return true;
			}


		/* Function: TryToSkipUnqualifiedIdentifier
		 *
		 * Tries to move past and retrieve a single unqualified identifier, which means only "X" in "X::Y::Z".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.Name>
		 *			  or <PrototypeParsingType.Type>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipUnqualifiedIdentifier (ref TokenIterator iterator, out string identifier, ParseMode mode = ParseMode.IterateOnly,
																		  PrototypeParsingType prototypeParsingType = PrototypeParsingType.Name)
			{
			TokenIterator start = iterator;

			if (TryToSkipUnqualifiedIdentifier(ref iterator, mode))
				{
				identifier = start.TextBetween(iterator);
				return true;
				}
			else
				{
				identifier = null;
				return false;
				}
			}


		/* Function: TryToSkipUnqualifiedIdentifier
		 *
		 * Tries to move the iterator past a single unqualified identifier, which means only "X" in "X::Y::Z".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.Name>
		 *			  or <PrototypeParsingType.Type>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipUnqualifiedIdentifier (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
																		  PrototypeParsingType prototypeParsingType = PrototypeParsingType.Name)
			{
			// Simple identifiers start with letters or underscores.  They can also contain numbers and $ but cannot start with
			// them.
			if (iterator.FundamentalType == FundamentalType.Text ||
				iterator.Character == '_')
				{
				if (iterator.Character >= '0' && iterator.Character <= '9')
					{  return false;  }

				TokenIterator startOfIdentifier = iterator;

				do
					{  iterator.Next();  }
				while (iterator.FundamentalType == FundamentalType.Text ||
						  iterator.Character == '_' ||
						  iterator.Character == '$');

				if (mode == ParseMode.ParsePrototype)
					{  startOfIdentifier.SetPrototypeParsingTypeBetween(iterator, prototypeParsingType);  }

				return true;
				}

			// Escaped identifiers start with \ and can contain any characters, including symbols.  They continue until the next
			// whitespace or line break.
			else if (iterator.Character == '\\')
				{
				TokenIterator startOfIdentifier = iterator;

				do
					{  iterator.Next();  }
				while (iterator.FundamentalType == FundamentalType.Text ||
						  iterator.FundamentalType == FundamentalType.Symbol);

				if (mode == ParseMode.ParsePrototype)
					{  startOfIdentifier.SetPrototypeParsingTypeBetween(iterator, prototypeParsingType);  }

				return true;
				}

			else
				{  return false;  }
			}



		// Group: Base Parsing Functions
		// __________________________________________________________________________


		/* Function: GenericSkip
		 *
		 * Advances the iterator one place through general code.
		 *
		 * - If the position is on a string, it will skip it completely.
		 * - If the position is on an opening brace, parenthesis, or bracket it will skip until the past the closing symbol.
		 * - Otherwise it skips one token.
		 */
		protected void GenericSkip (ref TokenIterator iterator)
			{
			if (iterator.MatchesAcrossTokens("(*"))
				{
				iterator.Next(2);
				GenericSkipUntilAfter(ref iterator, "*)");
				}
			else if (iterator.MatchesAcrossTokens("#("))
				{
				iterator.Next(2);
				GenericSkipUntilAfter(ref iterator, ')');
				}
			else if (iterator.Character == '(')
				{
				iterator.Next();
				GenericSkipUntilAfter(ref iterator, ')');
				}
			else if (iterator.Character == '[')
				{
				iterator.Next();
				GenericSkipUntilAfter(ref iterator, ']');
				}
			else if (iterator.Character == '{')
				{
				iterator.Next();
				GenericSkipUntilAfter(ref iterator, '}');
				}
			else if (TryToSkipString(ref iterator) ||
					  TryToSkipWhitespace(ref iterator))
				{  }
			else
				{  iterator.Next();  }
			}


		/* Function: GenericSkipUntilAfter
		 * Advances the iterator via <GenericSkip()> until a specific symbol is reached and passed.
		 */
		protected void GenericSkipUntilAfter (ref TokenIterator iterator, char symbol)
			{
			while (iterator.IsInBounds)
				{
				if (iterator.Character == symbol)
					{
					iterator.Next();
					break;
					}
				else
					{  GenericSkip(ref iterator);  }
				}
			}


		/* Function: GenericSkipUntilAfter
		 * Advances the iterator via <GenericSkip()> until a specific symbol is reached and passed.
		 */
		protected void GenericSkipUntilAfter (ref TokenIterator iterator, string symbol)
			{
			while (iterator.IsInBounds)
				{
				if (iterator.MatchesAcrossTokens(symbol))
					{
					iterator.NextByCharacters(symbol.Length);
					break;
					}
				else
					{  GenericSkip(ref iterator);  }
				}
			}


		/* Function: TryToSkipWhitespace
		 *
		 * For skipping whitespace and comments.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipWhitespace (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			int originalTokenIndex = iterator.TokenIndex;

			for (;;)
				{
				if (iterator.FundamentalType == FundamentalType.Whitespace ||
					iterator.FundamentalType == FundamentalType.LineBreak)
					{  iterator.Next();  }

				else if (TryToSkipComment(ref iterator, mode))
					{  }

				else
					{  break;  }
				}

			return (iterator.TokenIndex != originalTokenIndex);
			}


		/* Function: TryToSkipComment
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		new protected bool TryToSkipComment (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			return (TryToSkipLineComment(ref iterator, mode) ||
					  TryToSkipBlockComment(ref iterator, mode));
			}


		/* Function: TryToSkipLineComment
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		new protected bool TryToSkipLineComment (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.MatchesAcrossTokens("//"))
				{
				TokenIterator startOfComment = iterator;
				iterator.NextByCharacters(2);

				while (iterator.IsInBounds &&
						 iterator.FundamentalType != FundamentalType.LineBreak)
					{  iterator.Next();  }

				if (mode == ParseMode.SyntaxHighlight)
					{  startOfComment.SetSyntaxHighlightingTypeBetween(iterator, SyntaxHighlightingType.Comment);  }

				return true;
				}
			else
				{  return false;  }
			}


		/* Function: TryToSkipBlockComment
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		new protected bool TryToSkipBlockComment (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.MatchesAcrossTokens("/*"))
				{
				TokenIterator startOfComment = iterator;
				iterator.NextByCharacters(2);

				while (iterator.IsInBounds &&
						 iterator.MatchesAcrossTokens("*/") == false)
					{  iterator.Next();  }

				if (iterator.MatchesAcrossTokens("*/"))
					{  iterator.NextByCharacters(2);  }

				if (mode == ParseMode.SyntaxHighlight)
					{  startOfComment.SetSyntaxHighlightingTypeBetween(iterator, SyntaxHighlightingType.Comment);  }

				return true;
				}
			else
				{  return false;  }
			}


		/* Function: TryToSkipString
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipString (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '\"')
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			while (lookahead.IsInBounds)
				{
				if (lookahead.Character == '\\')
					{
					// This also covers line breaks
					lookahead.Next(2);
					}

				else if (lookahead.Character == '\"')
					{
					lookahead.Next();
					break;
					}

				else
					{  lookahead.Next();  }
				}


			if (lookahead.IsInBounds)
				{
				if (mode == ParseMode.SyntaxHighlight)
					{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.String);  }

				iterator = lookahead;
				return true;
				}
			else
				{  return false;  }
			}


		/* Function: TryToSkipNumber
		 *
		 * If the iterator is on a numeric literal, moves the iterator past it and returns true.  This covers:
		 *
		 *		- Simple numbers like 12 and 1.23
		 *		- Scientific notation like 1.2e3
		 *		- Numbers in different bases like 'H01AB and 'b1010
		 *		- Sized numbers like 4'b0011
		 *		- Digit separators like 1_000 and 'b0000_0011
		 *		- X, Z, and ? digits like 'H01XX
		 *		- Constants like '0, '1, 'X, and 'Z
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipNumber (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if ( (iterator.Character < '0' || iterator.Character > '9') &&
				   iterator.Character != '-' && iterator.Character != '+' && iterator.Character != '\'' )
				{  return false;  }

			TokenIterator lookahead = iterator;

			bool validNumber = false;
			TokenIterator endOfNumber = iterator;


			// Leading +/-, optional

			if (lookahead.Character == '-' || lookahead.Character == '+')
				{
				// Distinguish between -1 and x-1

				TokenIterator lookbehind = iterator;
				lookbehind.Previous();

				lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.Iterator);

				if (lookbehind.FundamentalType == FundamentalType.Text ||
					lookbehind.Character == '_' || lookbehind.Character == '$')
					{  return false;  }

				lookahead.Next();

				// This isn't enough to set validNumber to true but we have a new endOfNumber.
				endOfNumber = lookahead;
				}


			// Next 0-9.  This could be:
			//    - A full decimal value: [12]
			//    - Part of a floating point value before the decimal: [12].34
			//    - A full floating point value in scientific notation without a decimal: [12e3]
			//    - Part of a floating point value in scientific notation without a decimal but with an exponent sign: [12e]-3
			//    - The size of the type preceding the base signifier: [4]'b1001
			// Note that they can all contain digit separators (_) but cannot start with them.

			if (lookahead.Character >= '0' && lookahead.Character <= '9')
				{
				do
					{  lookahead.Next();  }
				while (lookahead.FundamentalType == FundamentalType.Text ||
						  lookahead.Character == '_');

				validNumber = true;
				endOfNumber = lookahead;
				}


			//	Check for a dot, which would continue a floating point number

			if (validNumber && lookahead.Character == '.')
				{
				lookahead.Next();

				// The part after a decimal can contain digit separators (_) but cannot start with them
				if (lookahead.Character >= '0' && lookahead.Character <= '9')
					{
					do
						{  lookahead.Next();  }
					while (lookahead.FundamentalType == FundamentalType.Text ||
							  lookahead.Character == '_');

					// The number is already marked as valid, so extend it to include this part
					endOfNumber = lookahead;
					}

				// If it wasn't a digit after the dot, the number is done and the dot isn't part of it
				else
					{  goto Done;  }
				}


			// Check for a +/-, which could continue a floating point number with an exponent

			if (validNumber &&
				(lookahead.Character == '+' || lookahead.Character == '-'))
				{
				// Make sure the character before this point was an E before we accept it.
				// We're safe to read RawTextIndex - 1 because we know we're not on its first character because validNumber
				// wouldn't be set otherwise.
				char previousCharacter = lookahead.Tokenizer.RawText[ lookahead.RawTextIndex - 1 ];

				if (previousCharacter == 'e' || previousCharacter == 'E')
					{
					lookahead.Next();

					if (lookahead.Character >= '0' && lookahead.Character <= '9')
						{
						do
							{  lookahead.Next();  }
						while (lookahead.FundamentalType == FundamentalType.Text ||
								  lookahead.Character == '_');

						// The number is already marked as valid, so extend it to include this part
						endOfNumber = lookahead;
						}

					// if there weren't digits after the E, end the number at the last valid point
					else
						{  goto Done;  }
					}

				// If it wasn't an E before the +/-, end the number at the last valid point
				else
					{  goto Done;  }
				}


			// There can be whitespace between a type size and the base signifier.

			if (validNumber)
				{  lookahead.NextPastWhitespace();  }


			// Check for an apostrophe, which could be:
			//   - A bit constant like '0 or 'Z
			//   - A base signifier like 'b
			// It can start a number so we don't need to check if it's already valid.

			if (lookahead.Character == '\'')
				{
				lookahead.Next();


				// Constants '0, '1, 'z, or 'x.  There is no '? constant.

				if (lookahead.RawTextLength == 1 &&
					(lookahead.Character == '0' || lookahead.Character == '1' ||
					 lookahead.Character == 'z' || lookahead.Character == 'Z' ||
					 lookahead.Character == 'x' || lookahead.Character == 'X'))
					{
					lookahead.Next();

					validNumber = true;
					endOfNumber = lookahead;

					goto Done;
					}


				// Base type signifiers: 'd, 'h, 'b, 'o, optionally preceded with s such as 'sd.

				char baseChar = lookahead.Character;
				bool baseIsSigned = false;

				if ((baseChar == 's' || baseChar == 'S') &&
					lookahead.RawTextLength > 1)
					{
					baseChar = lookahead.Tokenizer.RawText[ lookahead.RawTextIndex + 1 ];
					baseIsSigned = true;
					}

				if (baseChar == 'd' || baseChar == 'D' ||
					baseChar == 'h' || baseChar == 'H' ||
					baseChar == 'b' || baseChar == 'B' ||
					baseChar == 'o' || baseChar == 'O')
					{
					// There can be space between the base signifier and the value after it
					if ( (lookahead.RawTextLength == 1 && !baseIsSigned) ||
						 (lookahead.RawTextLength == 2 && baseIsSigned) )
						{
						lookahead.Next();
						lookahead.NextPastWhitespace();

						// Check that what's next is valid
						if ( (lookahead.Character >= '0' && lookahead.Character <= '9') ||
							  (lookahead.Character >= 'a' && lookahead.Character <= 'f') ||
							  (lookahead.Character >= 'A' && lookahead.Character <= 'F') ||
							  lookahead.Character == 'x' || lookahead.Character == 'X' ||
							  lookahead.Character == 'z' || lookahead.Character == 'Z' ||
							  lookahead.Character == '?' )
							{
							lookahead.Next();

							validNumber = true;
							endOfNumber = lookahead;
							}
						else
							{  goto Done;  }
						}

					// If the token was longer than the base signifer, assume it runs together with the value and is valid
					else
						{
						lookahead.Next();

						validNumber = true;
						endOfNumber = lookahead;
						}

					// Get the rest of the value's tokens.  If it wasn't valid we would have gone to Done already.
					while ( (lookahead.Character >= '0' && lookahead.Character <= '9') ||
							   (lookahead.Character >= 'a' && lookahead.Character <= 'f') ||
							   (lookahead.Character >= 'A' && lookahead.Character <= 'F') ||
								lookahead.Character == 'x' || lookahead.Character == 'X' ||
								lookahead.Character == 'z' || lookahead.Character == 'Z' ||
								lookahead.Character == '?' || lookahead.Character == '_' )
						{
						lookahead.Next();
						endOfNumber = lookahead;
						}
					}
				}


			// Time units.  There can whitespace between it and the value.

			if (validNumber)
				{
				lookahead.NextPastWhitespace();

				// Only lowercase is valid.
				if (lookahead.MatchesToken("s") ||
					lookahead.MatchesToken("ms") ||
					lookahead.MatchesToken("us") ||
					lookahead.MatchesToken("ns") ||
					lookahead.MatchesToken("ps") ||
					lookahead.MatchesToken("fs"))
					{
					lookahead.Next();
					endOfNumber = lookahead;
					}
				}


			Done:  // An evil goto target!  The shame!

			if (validNumber)
				{
				if (mode == ParseMode.SyntaxHighlight)
					{  iterator.SetSyntaxHighlightingTypeBetween(endOfNumber, SyntaxHighlightingType.Number);  }

				iterator = endOfNumber;
				return true;
				}
			else
				{  return false;  }
			}



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: Keywords
		 */
		static protected StringSet Keywords = new StringSet (KeySettings.Literal, new string[] {

			// Listed in the SystemVerilog 2017 reference's keywords section

			"accept_on", "alias", "always", "always_comb", "always_ff", "always_latch", "and", "assert", "assign", "assume",
			"automatic", "before", "begin", "bind", "bins", "binsof", "bit", "break", "buf", "bufif0", "bufif1", "byte", "case", "casex",
			"casez", "cell", "chandle", "checker", "class", "clocking", "cmos", "config", "const", "constraint", "context", "continue",
			"cover", "covergroup", "coverpoint", "cross", "deassign", "default", "defparam", "design", "disable", "dist", "do", "edge",
			"else", "end", "endcase", "endchecker", "endclass", "endclocking", "endconfig", "endfunction", "endgenerate", "endgroup",
			"endinterface", "endmodule", "endpackage", "endprimitive", "endprogram", "endproperty", "endspecify", "endsequence",
			"endtable", "endtask", "enum", "event", "eventually", "expect", "export", "extends", "extern", "final", "first_match", "for",
			"force", "foreach", "forever", "fork", "forkjoin", "function", "generate", "genvar", "global", "highz0", "highz1", "if", "iff",
			"ifnone", "ignore_bins", "illegal_bins", "implements", "implies", "import", "incdir", "include", "initial", "inout", "input",
			"inside", "instance", "int", "integer", "interconnect", "interface", "intersect", "join", "join_any", "join_none", "large", "let",
			"liblist", "library", "local", "localparam", "logic", "longint", "macromodule", "matches", "medium", "modport", "module",
			"nand", "negedge", "nettype", "new", "nexttime", "nmos", "nor", "noshowcancelled", "not", "notif0", "notif1", "null", "or",
			"output", "package", "packed", "parameter", "pmos", "posedge", "primitive", "priority", "program", "property", "protected",
			"pull0", "pull1", "pulldown", "pullup", "pulsestyle_ondetect", "pulsestyle_onevent", "pure", "rand", "randc", "randcase",
			"randsequence", "rcmos", "real", "realtime", "ref", "reg", "reject_on", "release", "repeat", "restrict", "return", "rnmos",
			"rpmos", "rtran", "rtranif0", "rtranif1", "s_always", "s_eventually", "s_nexttime", "s_until", "s_until_with", "scalared",
			"sequence", "shortint", "shortreal", "showcancelled", "signed", "small", "soft", "solve", "specify", "specparam", "static",
			"string", "strong", "strong0", "strong1", "struct", "super", "supply0", "supply1", "sync_accept_on", "sync_reject_on",
			"table", "tagged", "task", "this", "throughout", "time", "timeprecision", "timeunit", "tran", "tranif0", "tranif1", "tri", "tri0",
			"tri1", "triand", "trior", "trireg", "type", "typedef", "union", "unique", "unique0", "unsigned", "until", "until_with", "untyped",
			"use", "uwire", "var", "vectored", "virtual", "void", "wait", "wait_order", "wand", "weak", "weak0", "weak1", "while",
			"wildcard", "wire", "with", "within", "wor", "xnor", "xor",

			// Others found in syntax

			"$unit", "$root"

			});


		/* var: BuiltInTypes
		 */
		static protected StringSet BuiltInTypes = new StringSet (KeySettings.Literal, new string[] {

			"bit", "logic", "reg",
			"byte", "shortint", "int", "longint", "integer", "time",
			"shortreal", "real", "realtime",
			"enum",
			"string", "chandle", "event"

			});
		}
	}
