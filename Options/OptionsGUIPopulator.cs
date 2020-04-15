using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SolidUtils.Options;

namespace SolidUtils
{
    public static class OptionsGUIPopulator
    {
        private static List<ToolStripMenuItem> Convert(List<OptionBase> options, Action<OptionBase> onOptionChanged = null, bool skipChildOptions = true)
        {
            var res = new List<ToolStripMenuItem>();
            foreach (var optionsType in (OptionType[])Enum.GetValues(typeof(OptionType)))
            {
                var optionsOfType = options.Where(o => o.OptionType == optionsType).ToList();
                foreach (var option in optionsOfType)
                {
                    var m = Convert(option, onOptionChanged, skipChildOptions);
                    if (m == null)
                    {
                        continue;
                    }

                    //
                    // Add GUI options to special submenu 'GUI'
                    //
                    if (optionsType == OptionType.GUI && option.ParentOption == null)
                    {
                        var guiMenu = res.FirstOrDefault(oi => oi.Text == "GUI");
                        if (guiMenu == null)
                        {
                            guiMenu = new ToolStripMenuItem("GUI");
                            res.Add(guiMenu);
                        }
                        guiMenu.DropDownItems.Add(m);
                        res.Add(guiMenu);
                        continue;
                    }

                    //
                    // Add debug options to special submenu 'DEBUG'
                    //
                    //if (optionsType == OptionType.Debug)
                    //{
                    //    var debugMenu = res.FirstOrDefault(oi => oi.Text == "DEBUG");
                    //    if (debugMenu == null)
                    //    {
                    //        debugMenu = new ToolStripMenuItem("DEBUG");
                    //        res.Add(debugMenu);
                    //    }
                    //    debugMenu.DropDownItems.Add(m);
                    //    res.Add(debugMenu);
                    //    continue;
                    //}


                    res.Add(m);
                }
            }

            return res;
        }

        private static ToolStripMenuItem Convert(OptionBase o, Action<OptionBase> onOptionChanged = null, bool skipChildOptions = true)
        {
            // Hidde options that are invisible to user
            if (o.OptionType == OptionType.Hidden)
            {
                return null;
            }

            var isDebugOption = o.Caption.ToLower().Contains("debug") 
                || o.OptionType == OptionType.Debug;

            // Debug options is visible only on debug
            if (isDebugOption && !Shared.IsDebugMode)
            {
                return null;
            }

            if (skipChildOptions && o.ParentOption != null)
            {
                return null;
            }

            ToolStripMenuItem m = null;

            var oBool = o as OptionBool;
            if (oBool != null) m = Convert_OptionBool(oBool, o, onOptionChanged);

            var oEnum = o as IOptionEnum;
            if (oEnum != null) m = Convert_OptionEnum(oEnum, o, onOptionChanged);

            if (m != null)
            {
                // Add special prefix for all debug options
                if (isDebugOption)
                {
                    m.Text = "[DEBUG]   " + m.Text.Replace("Debug", "").Replace("[DEBUG]", "");
                }
            }

            return m;
        }


        private static Bitmap IconCheckBox_Checked = null;
        private static Bitmap IconCheckBox_Unchecked = null;
        private static Bitmap IconRadioBox_Checked = null;
        private static Bitmap IconRadioBox_Unchecked = null;

        public static Image GetCheckedBitmap(bool isChecked, bool checkBoxOrRadioBox)
        {
            if (IconCheckBox_Checked == null)
            {
                IconCheckBox_Checked = ResourceOptions.checkbox_checked7;
                IconCheckBox_Unchecked = ResourceOptions.checkbox_unchecked6;
                IconRadioBox_Checked = ResourceOptions.radiobox_checked2;
                IconRadioBox_Unchecked = ResourceOptions.radiobox_unchecked;
            }
            return checkBoxOrRadioBox
                ? (isChecked ? IconCheckBox_Checked : IconCheckBox_Unchecked)
                : (isChecked ? IconRadioBox_Checked : IconRadioBox_Unchecked);
        }

        private static void SetCheckedBitmap(ToolStripMenuItem m, bool isChecked, bool checkBoxOrRadioBox)
        {
            //m.Checked = isChecked;
            //m.CheckOnClick = true;
            m.Image = GetCheckedBitmap(isChecked, checkBoxOrRadioBox);
            //m.ImageScaling = ToolStripItemImageScaling.SizeToFit;
        }

        private static void SetEnabledToSubmenus(ToolStripMenuItem m, bool enabled)
        {
            if (m.DropDownItems.Count > 0)
            {
                foreach (ToolStripItem msub_ in m.DropDownItems)
                {
                    var msub = msub_ as ToolStripMenuItem;
                    if (msub == null) continue; // avoid ToolStripSeparator which is of type ToolStripItem
                    msub.Enabled = enabled;
                }
            }
        }

        private static void AddChildOptions(ToolStripMenuItem m, OptionBase o, Action<OptionBase> onOptionChanged)
        {
            if (o.ChildOptions.Count > 0)
            {
                var prevIsEnum = -1;
                foreach (var childOption in o.ChildOptions)
                {
                    var childMenu = Convert(childOption, onOptionChanged, false);
                    // some options may not return any menu - since they are debug and we run outside visual studio
                    if (childMenu == null)
                    {
                        continue;
                    }
                    if (childOption.OptionType == OptionType.IssueOption) childMenu.ForeColor = Color.SlateBlue;
                    if (childMenu == null) continue;
                    var oEnum = childOption as IOptionEnum;
                    if (oEnum == null
                        || (oEnum.GetStates().Length > 10 && o.ChildOptions.Count > 1)) //expantion is correct
                    {
                        if (prevIsEnum == 1) m.DropDownItems.Add(new ToolStripSeparator()); //add separator after OptionEnum
                        prevIsEnum = 0;
                        m.DropDownItems.Add(childMenu);
                    }
                    else
                    {
                        // Expand OptionEnum to avoid addition submenu level
                        if (prevIsEnum != -1) m.DropDownItems.Add(new ToolStripSeparator()); //add separator before OptionEnum
                        prevIsEnum = 1;
                        //m.DropDownItems.Add(new ToolStripMenuItem(childMenu.Text)); //caption             
                        while (childMenu.DropDownItems.Count > 0)
                        {
                            ToolStripItem msub  = childMenu.DropDownItems[0];
                            if (!msub.Text.Contains(":  "))
                            {
                                msub.Text = @"{0}:      {1}"._Format(childMenu.Text, msub.Text);
                            }
                            m.DropDownItems.Add(msub);
                        }
                    }
                }
            }
        }

        private static ToolStripMenuItem Convert_OptionBool(OptionBool oBool, OptionBase o, Action<OptionBase> onOptionChanged)
        {
            var m = new ToolStripMenuItem
            {
                Text = o.Caption,
                Tag = o,
                Image = GetCheckedBitmap(oBool.Value, true),
                
            };

            // Add childs
            AddChildOptions(m, o, onOptionChanged);
            SetEnabledToSubmenus(m, oBool.Value);

            m.Click += (eSender, eE) =>
            {
                var em = (ToolStripMenuItem)eSender;
                var eoBool = (OptionBool)em.Tag;
                eoBool.Value = !eoBool.Value;
                em.Image = GetCheckedBitmap(eoBool.Value, true);
                SetEnabledToSubmenus(em, eoBool.Value);
                if (onOptionChanged != null) onOptionChanged(eoBool);
            };
            return m;
        }

        private static ToolStripMenuItem Convert_OptionEnum(IOptionEnum oEnum, OptionBase o, Action<OptionBase> onOptionChanged)
        {
            var m = new ToolStripMenuItem
                            {
                                Text = o.Caption,
                                Tag = o,
                            };
            
            foreach (var state in oEnum.GetStates())
            {
                var msub = state.MenuCache; // improves performance 2 times
                if (msub == null)
                {
                    msub = new ToolStripMenuItem
                    {
                        Text = state.Caption,
                        Tag = state,
                    };
                    msub.Click += (eSender, eE) =>
                    {
                        var em = (ToolStripMenuItem) eSender;
                        var estate = (OptionEnumState) em.Tag;
                        var obase = (OptionBase) estate.OptionEnum;
                        var eoEnum = (IOptionEnum) estate.OptionEnum;
                        eoEnum.ClickedOnValue(estate.Value);
                        if (onOptionChanged != null) onOptionChanged(obase);
                    };
                    state.MenuCache = msub;
                }
                var image = GetCheckedBitmap(state.IsChecked, !oEnum.IsSingleValue);
                if (msub.Image != image) //  small performance improvement
                {
                    msub.Image = image;
                }
                m.DropDownItems.Add(msub);
            }
            return m;
        }

        public static void Populate(ToolStripItemCollection strips, Type relatedTo, Action<OptionBase> onOptionChanged = null)
        {
            var optionsRelatedTo = GlobalOptions.Options.Where(o => o.RelatedTo.Contains(relatedTo)).ToList();
            Populate(strips, optionsRelatedTo, onOptionChanged);
        }

        public static void Populate(ToolStripItemCollection strips, List<OptionBase> options, Action<OptionBase> onOptionChanged = null, bool skipChildOptions = true)
        {
            using (new log.GroupDEBUG(g.IssueFinder, "Populate popup menu", false))
            {
                strips.Clear();
                var menus = Convert(options, onOptionChanged, skipChildOptions);
                foreach (var menu in menus)
                {
                    if (menu != null)
                    {
                        strips.Add(menu);
                    }
                }
            }
        }

        public static void _FixPopupRenderBug(this ToolStripDropDownButton dropDownButton)
        {
            dropDownButton.DropDown = new ContextMenuStrip();
            dropDownButton.DropDown.Font = dropDownButton.Font; // fixes problem with font size
            dropDownButton.DropDown.ShowItemToolTips = false; // fixes problem with unopening some submenus
            // toolStripDropDownButtonOptions.DropDown.RenderMode = ToolStripRenderMode.ManagerRenderMode;
        }
    }
}
