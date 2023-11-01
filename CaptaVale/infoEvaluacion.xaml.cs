
using CaptaVale.Conexion;
using CaptaVale.Documentos;
using CaptaVale.ListViewClass;
using Microsoft.Data.SqlClient;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Views;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CaptaVale;

public partial class infoEvaluacion : ContentPage
{
    Prospecto info;
    private MyPageViewModelArchivos viewModel;
    public infoEvaluacion(Prospecto prospecto)
	{
        info = prospecto;
        InitializeComponent();
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

    private async void btnAprobarProspecto(object sender, EventArgs e)
    {
        bool result = await DisplayAlert("Aprobar", "¿Estás seguro de que va a aprobar a " + info.Nombre + " " + info.PrimerApellido + " " + info.SegundoApellido, "Sí", "No");

        if (result)
        {
            string updateEstatusQuery = "UPDATE [dbo].[Prospectos] SET Estatus = 'Aprobado' WHERE ID = @ProspectoID;";

            using (SqlConnection connection = ConexionSql.GetConnection())
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(updateEstatusQuery, connection))
                {
                    command.Parameters.AddWithValue("@ProspectoID", info.ProspectoID);

                    try
                    {
                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            info.Estatus = "Aprobado";
                        }
                        await DisplayAlert("EXITO", "El estatus del prospecto a cambiado", "Aceptar");
                        Application.Current.MainPage = new MainPage();
                    }
                    catch (Exception ex) { await DisplayAlert("ERROR", "Error al cambiar el estatus: " + ex.Message, "Aceptar"); }
                }
            }
        }
    }

    private async void btnRechazarProspecto(object sender, EventArgs e)
    {
        string observacion = await DisplayPromptAsync("Observación", "Escriba la razón por la que está rechazando a " + info.Nombre + " " + info.PrimerApellido + " " + info.SegundoApellido, "Aceptar", "Cancelar");

        if (!string.IsNullOrEmpty(observacion))
        {
            string updateEstatusQuery = "UPDATE [dbo].[Prospectos] SET Estatus = 'Rechazado', ObservacionRechazo = @ObservacionRechazo WHERE ID = @ProspectoID;";

            using (SqlConnection connection = ConexionSql.GetConnection())
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(updateEstatusQuery, connection))
                {
                    command.Parameters.AddWithValue("@ProspectoID", info.ProspectoID);
                    command.Parameters.AddWithValue("@ObservacionRechazo", observacion);

                    try
                    {
                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            info.Estatus = "Rechazado";
                        }
                        await DisplayAlert("EXITO", "El estatus del prospecto a cambiado", "Aceptar");
                        Application.Current.MainPage = new MainPage();
                    }
                    catch (Exception ex) { await DisplayAlert("ERROR", "Error al cambiar el estatus: " + ex.Message, "Aceptar"); }
                }
            }
        }
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
        if (documento != null && !viewModel.MyItems.Any(item => item.NombreDocumento == documento.NombreDocumento)) { viewModel.MyItems.Add(documento); }

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

                    // Abre el archivo con la aplicación predeterminada del dispositivo
                    await Launcher.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(rutaArchivo)
                    });
                }

            }
            else
            {
                await DisplayAlert("Error", "No se encontró un archivo para este prospecto", "Aceptar");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "Aceptar");
        }
    }
}