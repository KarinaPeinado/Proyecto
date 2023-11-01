using CaptaVale.Conexion;
using CaptaVale.ListViewClass;
using Microsoft.Data.SqlClient;

namespace CaptaVale;

public partial class evaluacionPage : ContentPage
{
    private MyPageViewModel viewModel;
    public List<Prospecto> Prospecto = new List<Prospecto>();
    private DateTime lastTapTime = DateTime.Now;
    public evaluacionPage()
	{
		InitializeComponent();
        LlenarListaProspectos();
    }
    private void btnRefresh_Clicked(object sender, EventArgs e)
    {
        LlenarListaProspectos();
    }
    private async void LlenarListaProspectos()
    {
        List<Prospecto> prospectos = new List<Prospecto>();
        string selectProspectoQuery = "SELECT * FROM [dbo].[Prospectos] WHERE Estatus = 'Enviado';";
        try
        {
            using (SqlConnection connection = ConexionSql.GetConnection())
            {
                using (SqlCommand command = new SqlCommand(selectProspectoQuery, connection))
                {
                    try
                    {
                        connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Prospecto prospecto = new Prospecto
                                {
                                    ProspectoID = reader.GetInt32(0),
                                    Nombre = reader.GetString(1),
                                    PrimerApellido = reader.GetString(2),
                                    SegundoApellido = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Estatus = reader.GetString(10)
                                };
                                prospectos.Add(prospecto);
                            }
                        }

                        viewModel = new MyPageViewModel(prospectos);
                        listViewProspEvaluacion.ItemsSource = viewModel.MyItems;
                        listViewProspEvaluacion.RowHeight = 120;
                        listViewProspEvaluacion.WidthRequest = 380;
                        listViewProspEvaluacion.Margin = new Thickness(10, 20, 10, 10);
                        //listViewProspEvaluacion.SeparatorColor = Colors.Black;
                        listViewProspEvaluacion.BackgroundColor = Colors.White;
                        listViewProspEvaluacion.SelectionMode = ListViewSelectionMode.None;
                        listViewProspEvaluacion.ItemTapped += OnItemTapped;

                        DataTemplate dataTemplate = new DataTemplate(() =>
                        {
                            ViewCell viewCell = new ViewCell();
                            Grid grid = new Grid();
                            grid.Padding = new Thickness(3);
                            grid.Margin = new Thickness(1);
                            grid.HorizontalOptions = LayoutOptions.Center;
                            grid.VerticalOptions = LayoutOptions.Center;

                            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

                            Label labelNombre = new Label();
                            labelNombre.SetBinding(Label.TextProperty, new Binding("Nombre", stringFormat: "Nombre: {0}"));
                            labelNombre.Margin = new Thickness(5, 5, 5, 5);
                            labelNombre.FontSize = 16;
                            labelNombre.FontAttributes = FontAttributes.Bold;
                            labelNombre.SetValue(Grid.ColumnSpanProperty, 2);
                            labelNombre.SetValue(Grid.ColumnProperty, 0);
                            labelNombre.SetValue(Grid.RowProperty, 0);
                            labelNombre.HorizontalOptions = LayoutOptions.Center;
                            labelNombre.VerticalOptions = LayoutOptions.Center;

                            Label labelPrimeroApellido = new Label();
                            labelPrimeroApellido.SetBinding(Label.TextProperty, new Binding("PrimerApellido", stringFormat: "Primer apellido: {0}"));
                            labelPrimeroApellido.Margin = new Thickness(5, 5, 5, 5);
                            labelPrimeroApellido.FontSize = 16;
                            labelPrimeroApellido.FontAttributes = FontAttributes.Bold;
                            labelPrimeroApellido.SetValue(Grid.ColumnProperty, 0);
                            labelPrimeroApellido.SetValue(Grid.RowProperty, 1);
                            labelPrimeroApellido.HorizontalOptions = LayoutOptions.Center;
                            labelPrimeroApellido.VerticalOptions = LayoutOptions.Center;

                            Label labelSegundoApellido = new Label();
                            labelSegundoApellido.SetBinding(Label.TextProperty, new Binding("SegundoApellido", stringFormat: "Segundo Apellido: {0}"));
                            labelSegundoApellido.Margin = new Thickness(5, 5, 5, 5);
                            labelSegundoApellido.FontSize = 16;
                            labelSegundoApellido.FontAttributes = FontAttributes.Bold;
                            labelSegundoApellido.SetValue(Grid.ColumnProperty, 1);
                            labelSegundoApellido.SetValue(Grid.RowProperty, 1);
                            labelSegundoApellido.HorizontalOptions = LayoutOptions.Center;
                            labelSegundoApellido.VerticalOptions = LayoutOptions.Center;

                            Label labelEstado = new Label();
                            labelEstado.SetBinding(Label.TextProperty, new Binding("Estatus", stringFormat: "Estatus: {0}"));
                            labelEstado.Margin = new Thickness(5, 5, 5, 5);
                            labelEstado.FontSize = 19;
                            labelEstado.FontAttributes = FontAttributes.Bold;
                            labelEstado.SetValue(Grid.ColumnSpanProperty, 2);
                            labelEstado.SetValue(Grid.ColumnProperty, 0);
                            labelEstado.SetValue(Grid.RowProperty, 2);
                            labelEstado.HorizontalOptions = LayoutOptions.Center;
                            labelEstado.VerticalOptions = LayoutOptions.Center;

                            grid.Children.Add(labelNombre);
                            grid.Children.Add(labelPrimeroApellido);
                            grid.Children.Add(labelSegundoApellido);
                            grid.Children.Add(labelEstado);

                            viewCell.View = grid;
                            return viewCell;
                        });
                        listViewProspEvaluacion.ItemTemplate = dataTemplate;
                    }
                    catch (Exception ex) { await DisplayAlert("ERROR", ex.Message, "Aceptar"); }
                }
            }
        }
        catch (Exception ex) { await DisplayAlert("ERROR", ex.Message, "Aceptar"); }
    }

    private async void OnItemTapped(object sender, ItemTappedEventArgs e)
    {
        var selectedItem = e.Item as Prospecto;

        Prospecto prospectoCompleto = ObtenerProspectoCompleto(selectedItem.ProspectoID);

        if (prospectoCompleto != null) { Application.Current.MainPage = new infoEvaluacion(prospectoCompleto); }
        else { await DisplayAlert("ERROR", "No se pudieron obtener los detalles del prospecto", "Aceptar"); }
    }


    private Prospecto ObtenerProspectoCompleto(int prospectoID)
    {
        string selectProspectoCompletoQuery = "SELECT * FROM [dbo].[Prospectos] WHERE ID = @ProspectoID";
        Prospecto prospectoCompleto = null;

        try
        {
            using (SqlConnection connection = ConexionSql.GetConnection())
            using (SqlCommand command = new SqlCommand(selectProspectoCompletoQuery, connection))
            {
                connection.Open();
                command.Parameters.AddWithValue("@ProspectoID", prospectoID);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        prospectoCompleto = new Prospecto
                        {
                            ProspectoID = reader.GetInt32(0),
                            Nombre = reader.GetString(1),
                            PrimerApellido = reader.GetString(2),
                            SegundoApellido = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Calle = reader.GetString(4),
                            Numero = reader.GetString(5),
                            Colonia = reader.GetString(6),
                            CodigoPostal = reader.GetInt32(7),
                            Telefono = reader.GetString(8),
                            RFC = reader.GetString(9),
                            Estatus = reader.GetString(10),
                            Observaciones = reader.IsDBNull(11) ? string.Empty : reader.GetString(11)
                        };
                    }
                }
            }
        }
        catch (Exception ex) { Console.WriteLine("Error al obtener prospecto completo: " + ex.Message); }
        return prospectoCompleto;
    }
}