/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.Targets.Output
 * ____________________________________________________________________________
 * 
 * A base class for the configuration of all output targets.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Engine.Config.Targets
	{
	abstract public class Output
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		public Output (PropertyLocation propertyLocation)
			{
			this.number = 0;

			this.overridableSettings = new OverridableOutputSettings();

			this.propertyLocation = propertyLocation;
			this.numberPropertyLocation = PropertySource.NotDefined;
			}


		public Output (Output toCopy)
			{
			this.number = toCopy.number;

			this.overridableSettings = new OverridableOutputSettings(toCopy.overridableSettings);

			this.propertyLocation = toCopy.propertyLocation;
			this.numberPropertyLocation = toCopy.numberPropertyLocation;
			}


		abstract public Output Duplicate ();


		/* Function: Validate
		 * Override to add errors if there are any problems with the target's properties, such as a folder not existing.
		 * TargetIndex is passed so that you may include it in the error's Property field, such as
		 * "InputTargets[0].Folder".
		 */
		public abstract bool Validate (Errors.ErrorList errorList, int targetIndex);


		/* Function: IsSameTarget
		 * Override to determine whether the two output targets are fundamentally the same.  Only primary identifying properties
		 * should be compared, so two <HTMLOutputFolders> should return true if they point to the same folder, and secondary
		 * properties like those in <OverridableSettings> are ignored.
		 */
		public abstract bool IsSameTarget (Output other);
			
	

		// Group: Properties
		// __________________________________________________________________________


		/* Property: Number
		 * The number of the output target, or zero if it isn't defined.  Numbers are used to determine the folder name each
		 * output target can use to save its working data.
		 */
		public int Number
			{
			get
				{  return number;  }
			set
				{  number = value;  }
			}


		/* Property: OverridableSettings
		 * The <OverridableOutputSettings> for this target.  These are settings that can be specified either here or in the
		 * <ProjectConfig>.  The ones here take precedence.  This object will always be defined, even if none of its properties 
		 * are.
		 */
		public OverridableOutputSettings OverridableSettings
			{
			get
				{  return overridableSettings;  }
			}


					
		// Group: Property Locations
		// __________________________________________________________________________


		/* Property: PropertyLocation
		 * Where the output target is defined.
		 */
		public PropertyLocation PropertyLocation
			{
			get
				{  return propertyLocation;  }
			}


		/* Property: NumberPropertyLocation
		 * Where <Number> is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation NumberPropertyLocation
		    {
		    get
		        {  return numberPropertyLocation;  }
		    set
		        {  numberPropertyLocation = value;  }
		    }



		// Group: Variables
		// __________________________________________________________________________
		

		protected int number;

		protected OverridableOutputSettings overridableSettings;

		protected PropertyLocation propertyLocation;
		protected PropertyLocation numberPropertyLocation;
				
		}
	}