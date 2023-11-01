using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CaptaVale.Documentos
{
    public class MyPageViewModelArchivos : INotifyPropertyChanged
    {
        private ObservableCollection<Archivo> item;
        public ObservableCollection<Archivo> MyItems
        {
            get { return item; }
            set { item = value; OnPropertyChanged(); }
        }

        public MyPageViewModelArchivos(List<Archivo> archivos)
        {
            MyItems = new ObservableCollection<Archivo>();

            foreach (Archivo archivo in archivos)
            { 
                MyItems.Add(new Archivo()
                {
                    NombreDocumento = archivo.NombreDocumento,
                    RutaDocumento = archivo.RutaDocumento
                }); ;
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
