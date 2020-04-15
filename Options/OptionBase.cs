using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Rhino;

namespace SolidUtils
{
    public enum OptionType
    {
        Default, Plugin, Autodraw, Add,
            Draw,              // Draw on viewport
            Zoom,
            GUI,                // Options that change GUI look. Will go to special menu 'GUI'.
            Log,
            Hidden,            // Hidde options is invisible to user
            Trick,               // Tricks options that in module SolidRhinoTricks
            Mesh,

            IssueGlobalOption,               // global options for issues.
            Issue,               // Issue enabled/disabled.
            IssueFinder,      // Finder enabled/disabled.
            IssueSeverity,    // Issue severity. Every issue have own severity.
            IssueComplexity,    // Issue complexity . Every issue have own complexity.
            IssueAutomated,    // Issue automation . Every issue have own automation flag.
            IssueOption,     // Issue options. Every issue can have options.
            
            Debug             // Debug options visible only for developers
    }
    public delegate void OnOptionChangeEvent(OptionBase option);
    public delegate void OnOptionChangeEventT<T>(OptionBaseT<T> option);

    public abstract class OptionBase
    {
        public string KeyShort { get; set; }
        public string KeyFull { get; set; }
        public string Caption { get; set; }
        public Type[] RelatedTo { get; set; }
        public OptionType OptionType { get; set; }
        public object IssueID { get; set; } // user data to help use this option

        public PersistentSettings Settings
        {
            get { return GlobalOptions.Settings; }
        }
        public OptionBase ParentOption { get; private set; }
        public List<OptionBase> ChildOptions = new List<OptionBase>();

        public OptionBase(string key, string caption, Type[] relatedTo, OptionType optionType)
        {
            KeyShort = key;
            KeyFull = key;
            if (relatedTo != null && relatedTo.Length > 0)
            {
                var a = relatedTo[0].Assembly.ManifestModule.Name.Replace(".dll", "").Replace(".rhp", "");
                KeyFull = a + "." + key;
                //log.temp(KeyFull);
            }

            Caption = !String.IsNullOrEmpty(caption) ? caption : key;
            RelatedTo = relatedTo;
            OptionType = optionType;

            GlobalOptions.Add(this);
        }

        public void AddChilds(params OptionBase[] childOptions)
        {
            foreach (var o in childOptions.Distinct())
            {
                o.ParentOption = this;
                ChildOptions.Add(o);
            }
        }

        /// <summary>
        /// When options is loaded at the start of application
        /// </summary>
        public event OnOptionChangeEvent OnLoaded;
        /// <summary>
        /// When options is loaded at application startup or changed by user.
        /// Is called after OnChange   (first OnChange, second OnModified)
        /// </summary>
        public event OnOptionChangeEvent OnModified;

        public abstract bool Load();
        public abstract bool Save();
        public abstract void ClearValue();

        internal void CallOnLoadEvent()
        {
            if (OnLoaded != null)
            {
                OnLoaded(this);
            }
            CallOnModified();
        }

        internal void CallOnModified()
        {
            if (OnModified != null)
            {
                OnModified(this);
            }
        }
    }



    [DebuggerDisplay("{Value} "+" - " + "{Caption}")]
    public abstract class OptionBaseT<T> : OptionBase
    {
        protected T _Value;
        protected T _PrevValue;

        public T OnChange_PrevValue
        {
            get { return _PrevValue; }
        }
        public T Value
        {
            get { return _Value; }
            set
            {
                var notEqualByNull = (_Value == null && value != null);
                if (notEqualByNull || (_Value != null && !_Value.Equals(value)))
                {
                    _PrevValue = _Value;
                    _Value = value;
                    CallOnChange();
                }
            }
        }

        internal void CallOnChange()
        {
            if (OnChangeInternal != null)
            {
                OnChangeInternal(this);
            }
            if (OnChange != null)
            {
                OnChange(this);
            }
            if (OnChangeT != null)
            {
                OnChangeT(this);
            }
            CallOnModified();
        }

        /// <summary>
        /// When option is changed by user.
        /// Is called before OnModified  (first OnChange, second OnModified)
        /// </summary>
        public event OnOptionChangeEvent OnChange;
        internal event OnOptionChangeEvent OnChangeInternal; // is made just to be called before call to OnChange event  - for inernal needs - to update values before call to OnChange
        /// <summary>
        /// When option is changed by user (event passed with options value).
        /// Is called before OnModified  (first OnChange, second OnModified)
        /// </summary>
        public event OnOptionChangeEventT<T> OnChangeT;
        /// <summary>
        /// When options is loaded at application startup or changed by user (event passed with options value).
        /// Is called after OnChange   (first OnChange, second OnModified)
        /// </summary>
        public event OnOptionChangeEventT<T> OnModifiedT;

        protected OptionBaseT(string key, T defaultValue, string caption, Type[] relatedTo, OptionType optionType)
            : base(key, caption, relatedTo, optionType)
        {
            _Value = defaultValue;
            OnModified += _OnModifiedT;
        }

        private void _OnModifiedT(OptionBase option)
        {
            if (OnModifiedT != null)
            {
                OnModifiedT(this);
            }
        }

        public override void ClearValue()
        {
            _Value = default(T);
        }

        //public override string ToString()
        //{
        //    return Value + " - " + Caption;
        //}
    }


}
