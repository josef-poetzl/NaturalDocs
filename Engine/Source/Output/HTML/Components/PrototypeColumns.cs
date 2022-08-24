﻿/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeColumns
 * ____________________________________________________________________________
 *
 * A class for storing information about the columns in one or more <Prototypes.ParameterSections>.
 *
 *
 * Threading: Not Thread Safe
 *
 *		This class is only designed to be used by one thread at a time.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public class PrototypeColumns
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: PrototypeColumns
		 * Creates a new columns object from the token indexes in a <PrototypeParameters> object.  This should only be called by
		 * <PrototypeParameters>.
		 */
		public PrototypeColumns (ParsedPrototype parsedPrototype, Prototypes.ParameterSection parameterSection, int[,] tokenIndexes)
			{
			parameterStyle = parameterSection.ParameterStyle;
			columnWidths = null;

			// DEPENDENCY: This function depends on the format of the tokenIndexes table generated by PrototypeParameters.
			CalculateColumnWidths(parsedPrototype, parameterSection, tokenIndexes);
			}


		/* Function: IsUsed
		 * Returns whether the column is used at all in any of the parameters.
		 */
		public bool IsUsed (int columnIndex)
			{
			if (columnIndex < columnWidths.Length)
				{  return (columnWidths[columnIndex] != 0);  }
			else
				{  throw new IndexOutOfRangeException();  }
			}


		/* Function: WidthOf
		 * Returns the width of the column in characters, which will be the width across all parameters.  Will return zero if the column
		 * is not used.
		 */
		public int WidthOf (int columnIndex)
			{
			if (columnIndex < columnWidths.Length)
				{  return columnWidths[columnIndex];  }
			else
				{  throw new IndexOutOfRangeException();  }
			}


		/* Function: TypeOf
		 * Returns the <PrototypeColumnType> of the column.
		 */
		public PrototypeColumnType TypeOf (int columnIndex)
			{
			if (columnIndex < columnWidths.Length)
				{  return Order[columnIndex];  }
			else
				{  throw new IndexOutOfRangeException();  }
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: CalculateColumnWidths
		 * Creates and fills in <columnWidths>.
		 */
		protected void CalculateColumnWidths (ParsedPrototype parsedPrototype, Prototypes.ParameterSection parameterSection, int[,] tokenIndexes)
			{
			columnWidths = new int[Count];


			// The first parameter just copy straight

			int parameter = 0;
			int column = 0;

			TokenIterator iterator = parsedPrototype.Tokenizer.FirstToken;
			iterator.Next( tokenIndexes[parameter, column] - iterator.TokenIndex );

			while (column < Count)
				{
				int startingCharOffset = iterator.CharNumber;

				iterator.Next( tokenIndexes[parameter, column+1] - iterator.TokenIndex );

				int endingCharOffset = iterator.CharNumber;

				columnWidths[column] = endingCharOffset - startingCharOffset;

				column++;
				}


			// Later parameters update if bigger

			for (parameter = 1; parameter < parameterSection.NumberOfParameters; parameter++)
				{
				column = 0;

				iterator.Next( tokenIndexes[parameter, column] - iterator.TokenIndex );

				while (column < Count)
					{
					int startingCharOffset = iterator.CharNumber;

					iterator.Next( tokenIndexes[parameter, column+1] - iterator.TokenIndex );

					int endingCharOffset = iterator.CharNumber;

					if (endingCharOffset - startingCharOffset > columnWidths[column])
						{  columnWidths[column] = endingCharOffset - startingCharOffset;  }

					column++;
					}
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Count
		 * Returns the number of columns used in the parameter style.
		 */
		public int Count
			{
			get
				{  return CountOf(parameterStyle);  }
			}


		/* Property: Order
		 * Returns an array of column types appropriate for the parameter style.  Do not change the data.
		 */
		public PrototypeColumnType[] Order
			{
			get
				{  return OrderOf(parameterStyle);  }
			}


		/* Property: FirstUsed
		 * Returns the index of the first used column, or -1 if there isn't one.
		 */
		 public int FirstUsed
			{
			get
				{
				for (int i = 0; i < columnWidths.Length; i++)
					{
					if (columnWidths[i] != 0)
						{  return i;  }
					}

				return -1;
				}
			}


		/* Property: LastUsed
		 * Returns the index of the last used column, or -1 if there isn't one.
		 */
		 public int LastUsed
			{
			get
				{
				for (int i = columnWidths.Length - 1; i >= 0; i--)
					{
					if (columnWidths[i] != 0)
						{  return i;  }
					}

				return -1;
				}
			}


		/* Property: ParameterStyle
		 * Returns the <ParsedPrototype.ParameterStyle> associated with the columns.
		 */
		public ParsedPrototype.ParameterStyle ParameterStyle
			{
			get
				{  return parameterStyle;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: parameterStyle
		 * The <ParsedPrototype.ParameterStyle> of the columns.
		 */
		protected ParsedPrototype.ParameterStyle parameterStyle;

		/* var: columnWidths
		 * An array of the column widths in characters.  Each one will be the longest width of that column in any individual
		 * parameter.  If the width is zero then that column is not used.
		 */
		protected int[] columnWidths;



		// Group: Static Functions
		// __________________________________________________________________________


		/* Function: CountOf
		 * Returns the number of columns in the passed <ParsedPrototype.ParameterStyle>.
		 */
		public static int CountOf (ParsedPrototype.ParameterStyle parameterStyle)
			{
			return OrderOf(parameterStyle).Length;
			}


		/* Function: OrderOf
		 * Returns an array of column types appropriate for the passed parameter style.  Do not change the data.
		 */
		public static PrototypeColumnType[] OrderOf (ParsedPrototype.ParameterStyle parameterStyle)
			{
			switch (parameterStyle)
				{
				case ParsedPrototype.ParameterStyle.C:
					return CColumnOrder;
				case ParsedPrototype.ParameterStyle.Pascal:
					return PascalColumnOrder;
				default:
					throw new NotSupportedException();
				}
			}



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: CColumnOrder
		 * An array of <PrototypeColumnTypes> representing the order in which columns should appear for C-style prototypes.
		 */
		static public PrototypeColumnType[] CColumnOrder = { PrototypeColumnType.ModifierQualifier,
																						 PrototypeColumnType.Type,
																						 PrototypeColumnType.Symbols,
																						 PrototypeColumnType.Name,
																						 PrototypeColumnType.PropertyValueSeparator,
																						 PrototypeColumnType.PropertyValue,
																						 PrototypeColumnType.DefaultValueSeparator,
																						 PrototypeColumnType.DefaultValue };

		/* var: PascalColumnOrder
		 * An array of <PrototypeColumnTypes> representing the order in which columns should appear for Pascal-style prototypes.
		 */
		static public PrototypeColumnType[] PascalColumnOrder = { PrototypeColumnType.ModifierQualifier,
																								PrototypeColumnType.Name,
																								PrototypeColumnType.TypeNameSeparator,
																								PrototypeColumnType.Symbols,
																								PrototypeColumnType.Type,
																								PrototypeColumnType.PropertyValueSeparator,
																								PrototypeColumnType.PropertyValue,
																								PrototypeColumnType.DefaultValueSeparator,
																								PrototypeColumnType.DefaultValue };
		}
	}
