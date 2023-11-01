using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CaptaVale.ListViewClass
{
    public class MyPageViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Prospecto> item;
        public ObservableCollection<Prospecto> MyItems
        {
            get { return item; }
            set { item = value; OnPropertyChanged(); }
        }
        public MyPageViewModel(List<Prospecto> prospectos)
        {
            MyItems = new ObservableCollection<Prospecto>();

            foreach (Prospecto prospecto in prospectos)
            {

                MyItems.Add(new Prospecto()
                {
                    ProspectoID = prospecto.ProspectoID,
                    Nombre = prospecto.Nombre,
                    PrimerApellido = prospecto.PrimerApellido,
                    SegundoApellido = prospecto.SegundoApellido,
                    Estatus = prospecto.Estatus
                });;
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
