using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel.Design.Serialization;
using System.ComponentModel;
namespace SolidUtils.GUI.StatusListProgress
{

	public class StatusItemConverter : TypeConverter
	{

		#region " Methods "

		// Get a boolean type determining whether or not the control can be converted to a status item
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destType)
		{
			if (object.ReferenceEquals(destType, typeof(InstanceDescriptor))) {
				return true;
			}

			return base.CanConvertTo(context, destType);
		}

		// Convert the specified control to a status item
		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destType)
		{
			if (object.ReferenceEquals(destType, typeof(InstanceDescriptor))) {
				System.Reflection.ConstructorInfo ci = typeof(StatusItem).GetConstructor(System.Type.EmptyTypes);

				return new InstanceDescriptor(ci, null, false);
			}

			return base.ConvertTo(context, culture, value, destType);
		}

		#endregion

	}
}
