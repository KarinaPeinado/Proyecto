using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptaVale.Documentos
{
    public class Archivo
    {
        public int ProspectoID { get; set; }
        public string NombreDocumento { get; set; }
        public string RutaDocumento { get; set; }
        public byte[] ContenidoDocumento { get; set; }
        public string urlDocumento { get; set; }
        public override string ToString()
        {
            return NombreDocumento;
        }
    }
}
