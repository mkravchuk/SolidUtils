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
	partial class StatusItem : System.ComponentModel.Component
	{

		[System.Diagnostics.DebuggerNonUserCode()]
		public StatusItem(System.ComponentModel.IContainer container) : this()
		{

			//Required for Windows.Forms Class Composition Designer support
			if ((container != null)) {
				container.Add(this);
			}

		}

		//Component overrides dispose to clean up the component list.
		[System.Diagnostics.DebuggerNonUserCode()]
		protected override void Dispose(bool disposing)
		{
			try {
				if (disposing && components != null) {
					components.Dispose();
				}
			} finally {
				base.Dispose(disposing);
			}
		}

		//Required by the Component Designer

		private System.ComponentModel.IContainer components;
		//NOTE: The following procedure is required by the Component Designer
		//It can be modified using the Component Designer.
		//Do not modify it using the code editor.
		[System.Diagnostics.DebuggerStepThrough()]
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}

	}
}
