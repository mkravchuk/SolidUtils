using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Rhino;

namespace SolidUtils
{
    public class OptionEnumState
    {
        public string Caption { get; internal set; }
        public object Value{ get; internal set; }
        public bool IsChecked { get; internal set; }
        public object OptionEnum { get; internal set; }
        public ToolStripMenuItem MenuCache { get; set; }

        public string ValueStr
        {
            get { return Value.ToString(); }
        }

        public override string ToString()
        {
            return (IsChecked ? "Y" : " ")
                + "   " + Value
                + "   " + Caption;
        }
    }
    public interface IOptionEnum
    {
        OptionEnumState[] GetStates();
        void ClickedOnValue(object value);
        bool IsSingleValue { get; }
    }

    public class OptionEnum<T> : OptionBaseT<T>, IOptionEnum
    {
        public bool IsSingleValue { get; private set; }
        public OptionEnumState[] States { get; private set; }

        public bool IsChecked(T value)
        {
            var state = States.SingleOrDefault(o => o.Value.Equals(value));
            if (state == null) return false;
            return state.IsChecked;
        }

        #region Constructor

        public OptionEnum(string key, string caption, Type[] relatedTo, OptionType optionType)
            : base( key, default(T), caption, relatedTo, optionType)
        {
            Init(new string[0], new T[0], new bool[0], true, false);
            OnChangeInternal += OnChangeInternalMy;
        }

        private void Init(string[] captions, T[] values, bool[] checkeds, bool isSingleValue, bool isBoolEnum)
        {
            IsSingleValue = isSingleValue;
            States = GetState(captions, values, checkeds, this);
        }

        public static implicit operator T(OptionEnum<T> option)
        {
            return option.Value;
        }

        //public static implicit operator bool(OptionEnum<T> option)
        //{
        //    return option.IsCheckedTrueValue;
        //}

        #endregion

        #region Init

        public OptionEnum<T> InitAsEnum(T defaultValue, string[] captions)
        {
            _Value = defaultValue;
            if (!typeof(T).IsEnum)
            {
                log.wrong("OptionEnum.InitAsEnum:  T is not enum");
                return this;
            }
            var values = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            var checkeds = values.Select(o => o.Equals(Value)).ToArray();
            Init(captions, values, checkeds, true, false);
            return this;
        }

        public OptionEnum<T> InitAsValues(T defaultValue, T[] values, string[] captions = null)
        {
            _Value = defaultValue;
            if (typeof(T).IsEnum)
            {
                log.wrong("OptionEnum.InitAsValues:  T is not a value");
                return this;
            }
            if (captions == null)
            {
                captions = values.Select(o => o.ToString()).ToArray();
            }
            var checkeds = values.Select(o => o.Equals(Value)).ToArray();
            Init(captions, values, checkeds, true, false);
            return this;
        }

        public OptionEnum<T> InitAsEnumArray(T[] defaultValue, string[] captions)
        {
            if (!typeof(T).IsEnum)
            {
                log.wrong("OptionEnum.InitAsEnumArray:  T is not a enum");
                return this;
            }

            //if (captions.Length != defaultValue.Length) - may differ, only few options from many can be checked
            //{
            //    log.wrong("OptionEnum: length of captions({0}) and defaultValue ({1}) should be same", captions.Length, defaultValue.Length);
            //    return this;
            //}

            var values = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            var checkeds = values.Select(o => defaultValue.Contains(o)).ToArray();

            Init(captions, values, checkeds, false, false);
            return this;
        }

        #endregion

        #region Load/Save

        public override bool Load()
        {
            string loadedVal;
            try
            {
                if (Settings.TryGetString(KeyFull, out loadedVal))
                {
                    if (IsSingleValue && String.IsNullOrEmpty(loadedVal))
                    {
                        return true;
                    }
                    var valuesStr = loadedVal.Split(',');
                    foreach (var state in States)
                    {
                        state.IsChecked = valuesStr.Contains(state.ValueStr);
                        if (state.IsChecked)
                        {
                            TryConvertStringToT(state.ValueStr, ref _Value);
                        }
                    }
                    return true;
                }

                
            }
            catch (Exception ex)
            {
                log.wrong("OptionEnumBase.Load  failed due to error: " + ex.Message);
            }
            return false;
        }

        

        public override bool Save()
        {
            var vals = States.Where(o => o.IsChecked).Select(o => o.ValueStr);
            Settings.SetString(KeyFull, String.Join(",", vals)); // good for single and multi values
            return true;
        }

        

        private bool TryConvertStringToT(string valueStr, ref T value)
        {
            foreach (var v in States)
            {
                if (v.ValueStr == valueStr)
                {
                    value = (T)v.Value;
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region States

        private static OptionEnumState[] GetState(string[] captions, T[] values, bool[] checkeds, object optionEnum)
        {
            if (captions.Length != values.Length
                || captions.Length != checkeds.Length)
            {
                log.wrong("OptionEnum.GetState   lengths should be same");
                return new List<OptionEnumState>().ToArray();
            }

            var res = new List<OptionEnumState>();
            for (int i = 0; i < values.Length; i++)
            {
                res.Add(new OptionEnumState
                {
                    Caption = captions[i],
                    Value = values[i],
                    IsChecked = checkeds[i],
                    OptionEnum = optionEnum
                });
            }
            return res.ToArray();
        }

        private void OnChangeInternalMy(OptionBase option)
        {
            // for the single value we have to sync States when Value is changed
            if (IsSingleValue)
            {
                // Update states checkeds from value
                foreach (var state in States)
                {
                    state.IsChecked = state.Value.Equals(_Value);
                }
            }
        }

        public OptionEnumState[] GetStates()
        {
            return States;
        }

        public void ClickedOnValue(object value)
        {
            if (IsSingleValue)
            {
                Value = (T)value; //here States will be updated in event call 'OnChangeInternalMy'
            }
            else
            {
                var state = States.Single(o => o.Value.Equals(value));
                state.IsChecked = !state.IsChecked;
                CallOnChange();
            }
        }

        #endregion


    }
}
