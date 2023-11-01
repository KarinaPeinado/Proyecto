using CaptaVale.Conexion;
using CaptaVale.Documentos;
using CaptaVale.ListViewClass;
using Microsoft.Data.SqlClient;
using System.Collections;
using System.Data;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using Azure.Storage.Blobs;
using Microsoft.WindowsAzure.Storage.Auth;

namespace CaptaVale;

public partial class clientePage : ContentPage
{
    public static ListView listView = new ListView();
    string rutaArchivo;
    public List<Archivo> documentos = new List<Archivo>();
    private MyPageViewModelArchivos viewModel;
    
    public clientePage()
	{
		InitializeComponent();
	}

    private async void OnPickFileButtonClicked(object sender, EventArgs e)
    {
        try
        {
            FileResult result;
            result = await FilePicker.PickAsync(new PickOptions { FileTypes = FilePickerFileType.Pdf });

            if (result != null)
            {
                string filePath = result.FullPath;
                rutaArchivo = filePath;
                string nombreDocumento = result.FileName;
                NombreArchivo_entry.Text = result.FileName;

                using (var fileStream = System.IO.File.OpenRead(filePath))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await fileStream.CopyToAsync(memoryStream);
                        var contenidoPDF = memoryStream.ToArray();

                        var archivo = new Archivo
                        {
                            NombreDocumento = nombreDocumento,
                            RutaDocumento = rutaArchivo,
                            ContenidoDocumento = contenidoPDF
                        };
                        documentos.Add(archivo);
                    }
                }

                listViewDocumentos.ItemsSource = null;
                viewModel = new MyPageViewModelArchivos(documentos);
                listViewDocumentos.ItemsSource = viewModel.MyItems;

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
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", ex.Message, "Aceptar");
        }
    }

    private async void btnInsert_Clicked(object sender, EventArgs e)
    {
        bool result = await DisplayAlert("Confirmar", "¿Estás seguro de que tus datos son correctos?", "Sí", "No");

        if (result)
        {
            ConexionSql dbConnection = new ConexionSql();

            try
            {
                if (string.IsNullOrWhiteSpace(Nombre_entry.Text) ||
                    string.IsNullOrWhiteSpace(PrimerApellido_entry.Text) ||
                    string.IsNullOrWhiteSpace(Calle_entry.Text) ||
                    string.IsNullOrWhiteSpace(Numero_entry.Text) ||
                    string.IsNullOrWhiteSpace(Colonia_entry.Text) ||
                    string.IsNullOrWhiteSpace(CodigoPostal_entry.Text) ||
                    string.IsNullOrWhiteSpace(Telefono_entry.Text) ||
                    string.IsNullOrWhiteSpace(RFC_entry.Text) ||
                    string.IsNullOrWhiteSpace(NombreArchivo_entry.Text))
                {
                    await DisplayAlert("Error", "Todos los campos son obligatorios.", "Aceptar");
                }
                else
                {
                    string nombre = Nombre_entry.Text;
                    string primerApellido = PrimerApellido_entry.Text;
                    string segundoApellido = SegundoApellido_entry.Text;
                    string calle = Calle_entry.Text;
                    string numero = Numero_entry.Text;
                    string colonia = Colonia_entry.Text;
                    int codigoPostal = int.Parse(CodigoPostal_entry.Text);
                    string telefono = Telefono_entry.Text;
                    string rfc = RFC_entry.Text;
                    string nombreArchivo = NombreArchivo_entry.Text;

                    if (dbConnection.OpenConnection())
                    {
                        using (SqlConnection connection = ConexionSql.GetConnection())
                        {
                            connection.Open();

                            string insertProspectoQuery = "INSERT INTO [dbo].[Prospectos] (Nombre, PrimerApellido, SegundoApellido, Calle, Numero, Colonia, CodigoPostal, Telefono, RFC, Estatus) " +
                                                        "VALUES (@Nombre, @PrimerApellido, @SegundoApellido, @Calle, @Numero, @Colonia, @CodigoPostal, @Telefono, @RFC, @Estatus); SELECT SCOPE_IDENTITY()";
                            int prospectoID;
                            using (SqlCommand command = new SqlCommand(insertProspectoQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Nombre", nombre);
                                command.Parameters.AddWithValue("@PrimerApellido", primerApellido);

                                if (string.IsNullOrEmpty(segundoApellido)) command.Parameters.AddWithValue("@SegundoApellido", DBNull.Value);                 
                                else command.Parameters.AddWithValue("@SegundoApellido", segundoApellido);

                                command.Parameters.AddWithValue("@Calle", calle);
                                command.Parameters.AddWithValue("@Numero", numero);
                                command.Parameters.AddWithValue("@Colonia", colonia);
                                command.Parameters.AddWithValue("@CodigoPostal", codigoPostal);
                                command.Parameters.AddWithValue("@Telefono", telefono);
                                command.Parameters.AddWithValue("@RFC", rfc);
                                command.Parameters.AddWithValue("@Estatus", "Enviado");

                                prospectoID = Convert.ToInt32(command.ExecuteScalar());
                            }

                            List<Archivo> archivos = documentos;
                            foreach (Archivo archivo in archivos)
                            {
                                string containerName = "docs";
                                string accountName = "captavale";
                                string accountKey = "2WaUOKsE9aZM9uTwDgwWWklQSM754mSvjDx8jMgawAXbYrE0C6otWOxnzn7/vuAeZderDiVfDfNd+AStOD6J6A==";
                                string blobName = archivo.NombreDocumento;
                                string blobUrl = $"https://{accountName}.blob.core.windows.net/{containerName}/{blobName}";

                                CloudBlockBlob blob = new CloudBlockBlob(new Uri(blobUrl), new StorageCredentials(accountName, accountKey));

                                string authorizationHeader = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy()
                                {
                                    Permissions = SharedAccessBlobPermissions.Read,
                                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1)
                                });

                                // Obtener un token de firma compartida
                                string sasToken = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy
                                {
                                    Permissions = SharedAccessBlobPermissions.Write,
                                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1), // Establece el tiempo de expiración adecuado
                                });

                                // Agregar el token de firma compartida al URI del blob
                                Uri blobUriWithSas = new Uri(blob.Uri, sasToken);

                                // Crear un blob con el URI que contiene el token de firma compartida
                                CloudBlockBlob blobWithSas = new CloudBlockBlob(blobUriWithSas);

                                using (var fileStream = new MemoryStream(archivo.ContenidoDocumento))
                                {
                                    await blobWithSas.UploadFromStreamAsync(fileStream);
                                }

                                //blobUrl += authorizationHeader;

                                string insertDocumentosQuery = "INSERT INTO [dbo].[Documentos] (ProspectoID, NombreDocumento, RutaDocumento, Contenido) " +
                                                               "VALUES (@ProspectoID, @NombreDocumento, @RutaDocumento, @Contenido)";

                                using (SqlCommand command = new SqlCommand(insertDocumentosQuery, connection))
                                {
                                    command.Parameters.AddWithValue("@ProspectoID", prospectoID);
                                    command.Parameters.AddWithValue("@NombreDocumento", archivo.NombreDocumento);
                                    command.Parameters.AddWithValue("@RutaDocumento", archivo.RutaDocumento);
                                    command.Parameters.AddWithValue("@Contenido", blobUrl);
                                    command.ExecuteNonQuery();
                                }
                            }
                            LimpiarCampos();
                        }
                    }

                    Prospecto nuevoProspecto = new Prospecto
                    {
                        Nombre = nombre,
                        PrimerApellido = primerApellido,
                        SegundoApellido = segundoApellido,
                        Calle = calle, 
                        Numero = numero,
                        Colonia = colonia,
                        CodigoPostal = codigoPostal,
                        Telefono = telefono,
                        RFC = rfc
                    };

                    await DisplayAlert("Éxito", "Tus datos se han enviado a revisión de forma correcta.", "Aceptar");
                    dbConnection.CloseConnection();
                    listViewDocumentos.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("ERROR", ex.Message, "Aceptar");
            }
        }
    }

    private async void btnClose_Clicked(object sender, EventArgs e)
    {
        bool result = await DisplayAlert("Salir", "¿Estás seguro de que quiere salir?. Se eliminara toda la informacipon ingresada y se cerrara la app.", "Sí", "No");

        if (result)
        {
            LimpiarCampos();
            System.Environment.Exit(0);
        }
    }

    private void LimpiarCampos()
    {
        Nombre_entry.Text = "";
        PrimerApellido_entry.Text = "";
        SegundoApellido_entry.Text = "";
        Calle_entry.Text = "";
        Numero_entry.Text = "";
        Colonia_entry.Text = "";
        CodigoPostal_entry.Text = "";
        Telefono_entry.Text = "";
        RFC_entry.Text = "";
        NombreArchivo_entry.Text = "";
        listViewDocumentos.ItemsSource = null;
    }

}