using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;

namespace SolidUtils.GUI.StatusListProgress
{
	public class StatusCollection : CollectionBase
	{

		#region " Declarations "

			// The StatusLabel control associated with this collection
		private StatusList Parent;

		#endregion

		#region " Methods "

		// Constructor
		internal StatusCollection(StatusList Parent)
		{
			// the status label control associated with this collection
			this.Parent = Parent;
		}

		// Gets the index of a specified item
		public int IndexOf(StatusItem Item)
		{
			return List.IndexOf(Item);
		}

		// Get the item at a specified index
		public StatusItem this[int Index] {
			get { return (StatusItem)List[Index]; }
		}

		// Check if the collection contains a specified item
		public bool Contains(StatusItem item)
		{
			return List.Contains(item);
		}

		// Adds a new statusitem to the collection
		public int Add(StatusItem item)
		{
			int i = 0;

			i = List.Add(item);
			item.Parent = Parent;
			Parent.DrawItems();

			return i;
		}

		// Removes a specified item from the collections
		public void Remove(StatusItem item)
		{
			List.Remove(item);
			item.Parent = null;
			Parent.DrawItems();
		}

		// Occurs when the collection has successfully added a new item.  For painting and validating purposes during design mode.
		protected override void OnInsertComplete(int index, object value)
		{
			base.OnInsert(index, value);

			Parent.DrawItems();
			Parent.Invalidate();
		}

		#endregion

	}
}
