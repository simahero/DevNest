using System;
using System.ComponentModel;

namespace DevNest.Core.Models
{
    public class DumpFile : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _path = string.Empty;
        private long _size;
        private DateTime _createdDate;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string Path
        {
            get => _path;
            set
            {
                if (_path != value)
                {
                    _path = value;
                    OnPropertyChanged(nameof(Path));
                }
            }
        }

        public long Size
        {
            get => _size;
            set
            {
                if (_size != value)
                {
                    _size = value;
                    OnPropertyChanged(nameof(Size));
                    OnPropertyChanged(nameof(SizeFormatted));
                }
            }
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set
            {
                if (_createdDate != value)
                {
                    _createdDate = value;
                    OnPropertyChanged(nameof(CreatedDate));
                }
            }
        }

        public string SizeFormatted
        {
            get
            {
                if (Size >= 1024 * 1024 * 1024)
                    return $"{Size / (1024.0 * 1024.0 * 1024.0):F2} GB";
                else if (Size >= 1024 * 1024)
                    return $"{Size / (1024.0 * 1024.0):F2} MB";
                else if (Size >= 1024)
                    return $"{Size / 1024.0:F2} KB";
                else
                    return $"{Size} bytes";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
