using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.ComponentModel;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Diarix.UserControls
{
    public sealed partial class HistoryCalendar : UserControl, INotifyPropertyChanged
    {
        private class DateTimeEqualityComparer : IEqualityComparer<DateTime>
        {
            public bool Equals(DateTime date1, DateTime date2) =>
                date1.Year == date2.Year && date1.Month == date2.Month && date1.Day == date2.Day;
            public int GetHashCode(DateTime date) => date.GetHashCode();
        }
        private static readonly DateTimeEqualityComparer equalityComparer = new DateTimeEqualityComparer();
        private IOrderedEnumerable<DateTime> _selectableDates;
        private DateTimeOffset _minDate, _maxDate;
        public IOrderedEnumerable<DateTime> SelectableDates
        {
            get => this._selectableDates;
            set
            {
                this._selectableDates = value;
                this.minDate = value?.Any() ?? false ? value.First() : DateTimeOffset.Now;
                this.maxDate = value?.Any() ?? false ? value.Last() : DateTimeOffset.Now;
            }
        }
        private DateTimeOffset minDate
        {
            get => this._minDate;
            set
            {
                this._minDate = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.minDate)));
            }
        }
        private DateTimeOffset maxDate
        {
            get => this._maxDate;
            set
            {
                this._maxDate = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.maxDate)));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<DateTime> DateSelected;
        public HistoryCalendar()
        {
            this.InitializeComponent();
            this.SelectableDates = null;
        }
        private void CalendarView_CalendarViewDayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
        {
            args.Item.IsBlackout = !(this.SelectableDates?.Contains(args.Item.Date.DateTime, equalityComparer) ?? false);
        }
        private void CalendarView_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            if (args.AddedDates.Any())
            {
                this.DateSelected?.Invoke(this, args.AddedDates.First().DateTime);
                sender.SelectedDates.Clear();
            }
        }
    }
}
