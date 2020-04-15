using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;
namespace SolidUtils.GUI.StatusListProgress
{

	internal class StatusListDesigner : ControlDesigner
	{

			// Keep track of which status label control we're attached to
		private StatusList Parent;

		public override void Initialize(System.ComponentModel.IComponent component)
		{
			base.Initialize(component);

			//Record instance of control we're designing
			Parent = (StatusList)component;

			//Hook up events
			ISelectionService s = (ISelectionService)GetService(typeof(ISelectionService));
			IComponentChangeService c = (IComponentChangeService)GetService(typeof(IComponentChangeService));
			s.SelectionChanged += OnSelectionChanged;
			c.ComponentRemoving += OnComponentRemoving;
		}

		private void OnSelectionChanged(object sender, System.EventArgs e)
		{
			// Call event on StatusLabel that a selection has changed
			Parent.OnSelectionChanged();
		}

		private void OnComponentRemoving(object sender, ComponentEventArgs e)
		{
			IComponentChangeService c = (IComponentChangeService)GetService(typeof(IComponentChangeService));
			StatusItem item = null;
			IDesignerHost h = (IDesignerHost)GetService(typeof(IDesignerHost));
			int i = 0;

			//If the user is removing a button
			if (e.Component is StatusItem) {
				item = (StatusItem)e.Component;
				if (Parent.Items.Contains(item)) {
					c.OnComponentChanging(Parent, null);
					Parent.Items.Remove(item);
					c.OnComponentChanged(Parent, null, null, null);
					return;
				}
			}

			//If the user is removing the control itself
			if (object.ReferenceEquals(e.Component, Parent)) {
				for (i = Parent.Items.Count - 1; i >= 0; i += -1) {
					item = Parent.Items[i];
					c.OnComponentChanging(Parent, null);
					Parent.Items.Remove(item);
					h.DestroyComponent(item);
					c.OnComponentChanged(Parent, null, null, null);
				}
			}

			Parent.DrawItems();
		}

		protected override void Dispose(bool disposing)
		{
			ISelectionService s = (ISelectionService)GetService(typeof(ISelectionService));
			IComponentChangeService c = (IComponentChangeService)GetService(typeof(IComponentChangeService));

			//Unhook events
			s.SelectionChanged -= OnSelectionChanged;
			c.ComponentRemoving -= OnComponentRemoving;

			base.Dispose(disposing);
		}

		public override System.Collections.ICollection AssociatedComponents {
			get { return Parent.Items; }
		}

		public override System.ComponentModel.Design.DesignerVerbCollection Verbs {
			get {
				DesignerVerbCollection v = new DesignerVerbCollection();

				//Verb to add buttons
				v.Add(new DesignerVerb("&Add Status Item", OnAddButton));

				return v;
			}
		}

		private void OnAddButton(object sender, EventArgs e)
		{
			StatusItem item = new StatusItem();
			IDesignerHost h = (IDesignerHost)GetService(typeof(IDesignerHost));
			DesignerTransaction dt = null;
			IComponentChangeService c = (IComponentChangeService)GetService(typeof(IComponentChangeService));

			//Add a new button to the collection
			dt = h.CreateTransaction("Add Status Item");
			item = (StatusItem)h.CreateComponent(typeof(StatusItem));
			c.OnComponentChanging(Parent, null);
			Parent.Items.Add(item);
			c.OnComponentChanged(Parent, null, null, null);
			dt.Commit();

			Parent.DrawItems();
		}

		protected override bool GetHitTest(System.Drawing.Point point)
		{
			StatusItem item = null;
			Rectangle wrct = default(Rectangle);

			point = Parent.PointToClient(point);

			foreach (StatusItem item_loopVariable in Parent.Items) {
				item = item_loopVariable;
				wrct = item.Bounds;

				// Check if the mouse has clicked on the item
				if (wrct.Contains(point))
					return true;
			}

			return false;
		}
	}
}
