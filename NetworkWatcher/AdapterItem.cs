using System.ComponentModel;

namespace NetworkWatcher
{
    public class AdapterItem : INotifyPropertyChanged
    {
        public required string Id { get; set; }  // required zajistí, že se hodnota nastaví před použitím
        public required string Name { get; set; }
        public required string Type { get; set; }
        public IReadOnlyList<string> MatchModes { get; } = new[] { "ID", "Name" };


    private bool isUp;
        public bool IsUp
        {
            get => isUp;
            set { isUp = value; OnPropertyChanged(nameof(IsUp)); }
        }

        private bool isIgnored;
        public bool IsIgnored
        {
            get => isIgnored;
            set { isIgnored = value; OnPropertyChanged(nameof(IsIgnored)); }
        }

        private string matchMode = "ID";
        public string MatchMode
        {
            get => matchMode;
            set { matchMode = value; OnPropertyChanged(nameof(MatchMode)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

}
