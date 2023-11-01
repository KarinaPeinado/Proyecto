
using CaptaVale.Conexion;
using CaptaVale.Documentos;
using CaptaVale.ListViewClass;
using Microsoft.Data.SqlClient;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Net;
using System.Reflection.Metadata;
using System.Text;

namespace CaptaVale;

public partial class infoProspectoPage : ContentPage
{
    Prospecto info;
    private DateTime lastTapTime = DateTime.Now;
    private MyPageViewModelArchivos viewModel;
    public infoProspectoPage(Prospecto prospecto)
	{
		InitializeComponent();
        info = prospecto;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        cargarDatos();
        ObtenerArchivoPorProspectoID();
    }
    private void Regresar(object sender, EventArgs e)
    {
        Application.Current.MainPage = new MainPage();
    }
    private void cargarDatos()
    {
        Nombre_entry.Text = info.Nombre;
        PrimerApellido_entry.Text = info.PrimerApellido;
        SegundoApellido_entry.Text = info.SegundoApellido;
        Calle_entry.Text = info.Calle;
        Numero_entry.Text = info.Numero;
        Colonia_entry.Text = info.Colonia;
        CodigoPostal_entry.Text = info.CodigoPostal.ToString();
        Telefono_entry.Text = info.Telefono;
        RFC_entry.Text = info.RFC;
        labelEstatus.Text = "Estatus: " + info.Estatus;
        if (info.Observaciones == null)
        {
            labelObservaciones.IsVisible = false;
        }
        else
        {
            labelObservaciones.Text = "Observaciones: " + info.Observaciones;
        }
    }
    public Archivo ObtenerArchivoPorProspectoID()
    {
        Archivo archivo = null;

        using (SqlConnection connection = ConexionSql.GetConnection())
        {
            connection.Open();

            string query = "SELECT NombreDocumento, RutaDocumento, Contenido FROM [dbo].[Documentos] WHERE ProspectoID = @ProspectoID";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ProspectoID", info.ProspectoID);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        archivo = new Archivo
                        {
                            NombreDocumento = reader["NombreDocumento"].ToString(),
                            RutaDocumento = reader["RutaDocumento"].ToString(),
                            urlDocumento = reader["Contenido"].ToString()
                        };
                    }
                }
            }
        }
        MostrarArchivoEnListView(archivo);
        return archivo;
    }

    private void MostrarArchivoEnListView(Archivo documento)
    {
        if (viewModel == null)
        {
            viewModel = new MyPageViewModelArchivos(new List<Archivo>());
            listViewDocumentos.ItemsSource = viewModel.MyItems;
        }

        // Verifica si el documento no est� ya en la lista
        if (documento != null && !viewModel.MyItems.Any(item => item.NombreDocumento == documento.NombreDocumento))
        {
            viewModel.MyItems.Add(documento);
        }
        /* if (viewModel == null)
         {
             viewModel = new MyPageViewModelArchivos(new List<Archivo>());
             listViewDocumentos.ItemsSource = viewModel.MyItems;
         }
         if (documento != null && !viewModel.MyItems.Contains(documento))
         {
             viewModel.MyItems.Add(documento);
         }*/

        var dataTemplate = new DataTemplate(() =>
        {
            var textCell = new TextCell();
            textCell.SetBinding(TextCell.TextProperty, "NombreDocumento");
            textCell.TextColor = Colors.Black;
            return textCell;
        });

        listViewDocumentos.ItemTemplate = dataTemplate;
        listViewDocumentos.RowHeight = 30;
        listViewDocumentos.Margin = new Thickness(10, 10, 10, 10);
        listViewDocumentos.SeparatorColor = Colors.Transparent;
        listViewDocumentos.BackgroundColor = Color.FromRgba(255, 255, 255, 255);
        listViewDocumentos.SelectionMode = ListViewSelectionMode.None;
        listViewDocumentos.ItemTapped += OnItemTapped;
    }

    private async void OnItemTapped(object sender, ItemTappedEventArgs e)
    {
        var selectedItem = e.Item as Archivo;

        try
        {
            Archivo archivo = ObtenerArchivoPorProspectoID();

            if (archivo != null)
            {
                string containerName = "docs";
                string accountName = "captavale";
                string accountKey = "2WaUOKsE9aZM9uTwDgwWWklQSM754mSvjDx8jMgawAXbYrE0C6otWOxnzn7/vuAeZderDiVfDfNd+AStOD6J6A==";
                string blobName = selectedItem.NombreDocumento;
                string blobUrl = $"https://{accountName}.blob.core.windows.net/{containerName}/{blobName}";

                CloudBlockBlob blob = new CloudBlockBlob(new Uri(blobUrl), new StorageCredentials(accountName, accountKey));

                // Obtener un token de firma compartida (SAS token) para acceso de solo lectura
                string sasToken = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1)
                });

                // Crear un URI que incluye el token de firma compartida
                Uri blobUriWithSas = new Uri(blob.Uri, sasToken);

                // Obtener los datos del Blob
                using (var memoryStream = new MemoryStream())
                {
                    await blob.DownloadToStreamAsync(memoryStream);

                    // Guardar los datos en un archivo local
                    string nombreArchivo = archivo.NombreDocumento;
                    string rutaArchivo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nombreArchivo);
                    File.WriteAllBytes(rutaArchivo, memoryStream.ToArray());

                    // Abre el archivo con la aplicaci�n predeterminada del dispositivo
                    await Launcher.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(rutaArchivo)
                    });
                }

            }
            else
            {
                await DisplayAlert("Error", "No se encontr� un archivo para este prospecto", "Aceptar");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "Aceptar");
        }
    }
}