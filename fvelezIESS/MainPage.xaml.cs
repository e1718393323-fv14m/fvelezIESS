using System.Globalization;
using System.Text;

namespace fvelezIESS;

public partial class MainPage : ContentPage
{
    // Porcentaje de aporte personal del afiliado al IESS
    private const decimal PorcentajeAporte = 0.0945m;

    private const string NombreArchivo = "aportes_iess.txt";
    private static string RutaArchivo => Path.Combine(FileSystem.AppDataDirectory, NombreArchivo);

    public MainPage()
    {
        InitializeComponent();
    }

    // ===== CONTROL: Edad solo admite dígitos =====
    private void OnEdadTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.NewTextValue))
            return;

        string limpio = new(e.NewTextValue.Where(char.IsDigit).ToArray());
        if (limpio != e.NewTextValue)
            txtEdad.Text = limpio;
    }

    // ===== CONTROL: Salario solo admite dígitos y un separador decimal =====
    private void OnSalarioTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.NewTextValue))
            return;

        var sb = new StringBuilder();
        bool tieneSeparador = false;
        int decimales = 0;

        foreach (char c in e.NewTextValue)
        {
            if (char.IsDigit(c))
            {
                if (tieneSeparador)
                {
                    if (decimales == 2) continue; // máximo 2 decimales
                    decimales++;
                }
                sb.Append(c);
            }
            else if ((c == '.' || c == ',') && !tieneSeparador)
            {
                tieneSeparador = true;
                sb.Append('.');
            }
        }

        string limpio = sb.ToString();
        if (limpio != e.NewTextValue)
            txtSalario.Text = limpio;
    }

    private async void OnCalcularClicked(object sender, EventArgs e)
    {
        // 1. Verificar que las cajas de texto no estén vacías
        if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
            string.IsNullOrWhiteSpace(txtApellido.Text) ||
            string.IsNullOrWhiteSpace(txtEdad.Text) ||
            string.IsNullOrWhiteSpace(txtSalario.Text))
        {
            await DisplayAlert("Error", "Todos los campos son obligatorios", "OK");
            return;
        }

        string nombre = txtNombre.Text.Trim();
        string apellido = txtApellido.Text.Trim();

        // 2. Validar que nombre y apellido tengan solo letras
        if (!nombre.All(c => char.IsLetter(c) || c == ' ') ||
            !apellido.All(c => char.IsLetter(c) || c == ' '))
        {
            await DisplayAlert("Error", "El nombre y el apellido solo admiten letras", "OK");
            return;
        }

        // 3. Validar edad (solo números y rango lógico)
        if (!int.TryParse(txtEdad.Text, out int edad) || edad < 15 || edad > 100)
        {
            await DisplayAlert("Error", "Ingrese una edad válida (entre 15 y 100)", "OK");
            return;
        }

        // 4. Validar salario (solo números, mayor a cero)
        string textoSalario = txtSalario.Text.Replace(',', '.');
        if (!decimal.TryParse(textoSalario, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal salario) || salario <= 0)
        {
            await DisplayAlert("Error", "Ingrese un salario válido mayor a 0", "OK");
            return;
        }

        // 5. Calcular el aporte al IESS (9,45%)
        decimal aporte = Math.Round(salario * PorcentajeAporte, 2, MidpointRounding.AwayFromZero);
        string aporteTexto = aporte.ToString("0.00", CultureInfo.InvariantCulture);
        string salarioTexto = salario.ToString("0.00", CultureInfo.InvariantCulture);

        // 6. Mostrar la alerta (saltos de línea con \n)
        string mensaje = $"Bienvenido {nombre} {apellido}\n" +
                         $"Tienes {edad} años\n" +
                         $"Tu aporte mensual es ${aporteTexto}";

        // 7. Enviar todo a un archivo de texto
        var sb = new StringBuilder();
        sb.AppendLine("===== CÁLCULO APORTE IESS =====");
        sb.AppendLine($"Nombre: {nombre}");
        sb.AppendLine($"Apellido: {apellido}");
        sb.AppendLine($"Edad: {edad} años");
        sb.AppendLine($"Salario: ${salarioTexto}");
        sb.AppendLine("Porcentaje de aporte: 9,45%");
        sb.AppendLine($"Aporte mensual: ${aporteTexto}");
        sb.AppendLine($"Fecha de registro: {DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine();

        try
        {
            await File.AppendAllTextAsync(RutaArchivo, sb.ToString(), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo guardar el archivo:\n{ex.Message}", "OK");
            return;
        }

        await DisplayAlert("Aporte IESS", mensaje + $"\n\nGuardado en:\n{RutaArchivo}", "OK");

        // 8. Limpiar campos
        txtNombre.Text = "";
        txtApellido.Text = "";
        txtEdad.Text = "";
        txtSalario.Text = "";
        txtNombre.Focus();
    }
}
