using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolidUtils
{
    public class EnumStateInfo
    {
        public string Caption;
        private bool _isChecked;
        public bool IsChecked
        {
            get
            {
                if (IsCheckedHook != null)
                {
                    var hookVal = IsCheckedHook(LocalUniqueID);
                    if (hookVal != -1)
                    {
                        return hookVal == 1;
                    }
                }
                return _isChecked;
            }
            set { _isChecked = value; }
        }
        public bool IsCheckedByDefault;
        public int Priority;

        public object TValue;
        public List<OptionBase> Options;

        internal int LocalUniqueID;
        internal int RuntimeSerialNumber;
        internal Func<int, int> IsCheckedHook;

        public IssueWeight IssueWeight;
        public void SetWeight(IssueSeverityType severity, int complexity, bool isAutomateFixAllowed)
        {
            IssueWeight = new IssueWeight
            {
                Severity = severity,
                Complexity = complexity,
                IsAutomateFixAllowed = isAutomateFixAllowed
            };
        }
    }

    public class EnumInfo<T>
    {
        private int NextLocalUniqueID;
        private int NextRuntimeSerialNumber;
        private readonly Dictionary<T, EnumStateInfo> Infos = new Dictionary<T, EnumStateInfo>();
        public event Func<T, bool> IsCheckedHook;

        public void Remove(T t)
        {
            Infos.Remove(t);
        }

        public EnumStateInfo this[T t]
        {
            get { return Infos[t]; }
        }

        public void ValidateEnumFullness()
        {
            if (typeof (T).IsEnum)
            {
                foreach (T t in Enum.GetValues(typeof(T)))
                {
                    if (!Infos.ContainsKey(t))
                    {
                        Add(t, false, t.ToString());
                        log.wrong("EnumInfo: Missed definition of enum for '{0}'", t);
                    }
                    if (Infos[t].IssueWeight == null)
                    {
                        Infos[t].SetWeight(IssueSeverityType.Hint, 1, false);
                        log.wrong("EnumInfo: Missed definition of IssueWeight for '{0}'", t);
                    }
                }
            }
        }

        public EnumStateInfo Add(T t, bool isChecked, string caption, int? priority = null, Action InitSettings = null)
        {
            if (!priority.HasValue)
            {
                priority = -(NextLocalUniqueID + 1); // we must use default priority different for all enums to get consistent sort order all the time
            }
            var options = new List<OptionBase>();
            if (InitSettings != null)
            {
                var countBeforeInit = GlobalOptions.Options.Count;
                InitSettings();
                options = GlobalOptions.Options.GetRange(countBeforeInit, GlobalOptions.Options.Count - countBeforeInit);
            }

            NextLocalUniqueID++;
            NextRuntimeSerialNumber++;
            Infos[t] = new EnumStateInfo
            {
                TValue = t, 
                Caption = caption,
                IsChecked = isChecked,
                IsCheckedByDefault = isChecked,
                Priority = priority.Value,
                LocalUniqueID = NextLocalUniqueID,
                RuntimeSerialNumber = NextRuntimeSerialNumber,
                IsCheckedHook = EnumStateInfo_IsCheckedHook,
                Options = options
            };
            return Infos[t];
        }

        public void InitSettings(OptionBool parent)
        {
            foreach (var t in Infos.Keys)
            {
                var tStr = t.ToString();
                if (tStr == "TrimControlPointsNotCorrectInSeam2")
                {
                    var temp = 0;
                }

                var info = this[t];

                // Add issue option
                var o = new OptionBool(typeof (T).Name + "." + t, info.IsCheckedByDefault, info.Caption, IssueOptions.RelatedTo, OptionType.Issue);
                o.IssueID = t;
                o.OnModifiedT += (option) => Infos[(T)option.IssueID].IsChecked = option.Value;

                // Add Automate child option
                var oAutomate = new OptionBool(o.KeyShort + ".Automated", info.IssueWeight.IsAutomateFixAllowed, "Is automated fix allowed", IssueOptions.RelatedTo, OptionType.IssueAutomated);
                oAutomate.IssueID = t;
                oAutomate.OnModifiedT += (option) => Infos[(T)option.IssueID].IssueWeight.IsAutomateFixAllowed = option.Value;
                o.AddChilds(oAutomate);

                // Add Complexity child option
                var oComplexity = new OptionEnum<int>(o.KeyShort + ".Complexity", "Complexity", IssueOptions.RelatedTo, OptionType.IssueComplexity)
                    .InitAsValues(info.IssueWeight.Complexity, new[]{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 20, 25, 30, 40, 50, 60});
                oComplexity.IssueID = t;
                oComplexity.OnModifiedT += (option) => Infos[(T)option.IssueID].IssueWeight.Complexity = option.Value;
                o.AddChilds(oComplexity);

                // Add Severity child option
                var oSeverity = new OptionEnum<IssueSeverityType>(o.KeyShort + ".Severity", "Severity", IssueOptions.RelatedTo, OptionType.IssueSeverity)
                    .InitAsEnum(info.IssueWeight.Severity, IssueSeverityTypeManager.GetCaptions());
                oSeverity.IssueID = t;
                oSeverity.OnModifiedT += (option) => Infos[(T)option.IssueID].IssueWeight.Severity = option.Value;
                o.AddChilds(oSeverity);

                // Add child options
                parent.AddChilds(o);
                foreach(var issueOption in info.Options)
                {
                    issueOption.IssueID = t;
                    o.AddChilds(issueOption);
                }

            }
        }

        private int EnumStateInfo_IsCheckedHook(int localUniqueID)
        {
            if (IsCheckedHook != null)
            {
                foreach (var t in Infos.Keys)
                {
                    if (Infos[t].LocalUniqueID == localUniqueID)
                    {
                        return IsCheckedHook(t) ? 1 : 0;
                    }
                }
            }
            return -1;
        }

        public T[] CheckedByDefault
        {
            get { return Infos.Keys.Where(t => Infos[t].IsCheckedByDefault).ToArray(); }
        }

        public string[] Captions
        {
            get { return Infos.Keys.Select(t => Infos[t].Caption).ToArray(); }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Infos.Keys.GetEnumerator();
        }

        public void SetIsChecked(Func<T, bool> isChecked)
        {
            foreach (var t in Infos.Keys)
            {
                Infos[t].IsChecked = isChecked(t);
            }
        }
    }

}
