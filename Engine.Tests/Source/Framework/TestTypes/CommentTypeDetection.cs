﻿/*
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.CommentTypeDetection
 * ____________________________________________________________________________
 *
 * File-based tests to detect the comment types of <Topics>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using CodeClear.NaturalDocs.Engine;
using CodeClear.NaturalDocs.Engine.Tests.Framework;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes
	{
	public class CommentTypeDetection : Framework.SourceToTopics
		{

		public override string OutputOf (IList<Topic> topics)
			{
			if (topics == null || topics.Count == 0)
				{  return "(No topics found)";  }

			StringBuilder output = new StringBuilder();

			bool isFirst = true;
			for (int i = 0; i < topics.Count; i++)
				{
				if (topics[i].IsEmbedded == false)
					{
					if (isFirst)
						{  isFirst = false;  }
					else
						{  output.AppendLine();  }

					output.AppendLine(topics[i].Title);
					output.AppendLine( "- Comment Type: " + EngineInstance.CommentTypes.FromID(topics[i].CommentTypeID).Name );
					output.AppendLine( "- Line " + topics[i].CommentLineNumber );
					}
				}

			return output.ToString();
			}

		}
	}
