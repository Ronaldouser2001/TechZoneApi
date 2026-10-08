using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configuración de puerto para Render / Docker
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();
app.UseCors("AllowAll");

// Mensaje en la raíz para verificar que la API esté viva
app.MapGet("/", () => Results.Ok(new { status = "success", message = "API TechZone activa y lista" }));

// Helper de lectura de JSON corregido con ruta AppContext.BaseDirectory
T ReadJson<T>(string fileName)
{
    var baseDir = AppContext.BaseDirectory;
    var path = Path.Combine(baseDir, "Data", fileName);
    
    // Si no está en BaseDirectory, busca en la raíz de ejecución
    if (!File.Exists(path))
    {
        path = Path.Combine(Directory.GetCurrentDirectory(), "Data", fileName);
    }

    if (!File.Exists(path))
    {
        throw new FileNotFoundException($"No se encontró el archivo JSON en: {path}");
    }

    var json = File.ReadAllText(path);
    return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
}

// Endpoints API
app.MapGet("/api/productos", (int? categoria_id, string? search) =>
{
    try
    {
        var productos = ReadJson<List<Producto>>("productos.json");
        var query = productos.AsEnumerable();
        if (categoria_id.HasValue) query = query.Where(p => p.Categoria_Id == categoria_id.Value);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.Nombre.Contains(search, StringComparison.OrdinalIgnoreCase));
        return Results.Ok(new { status = "success", count = query.Count(), data = query.ToList() });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/api/productos/{id:int}", (int id) =>
{
    try
    {
        var productos = ReadJson<List<Producto>>("productos.json");
        var producto = productos.FirstOrDefault(p => p.Id == id);
        return producto is not null ? Results.Ok(new { status = "success", data = producto }) : Results.NotFound(new { status = "error", message = "Producto no encontrado" });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/api/categorias", () =>
{
    try { return Results.Ok(new { status = "success", data = ReadJson<List<Categoria>>("categorias.json") }); }
    catch (Exception ex) { return Results.Problem(ex.Message); }
});

app.MapGet("/api/promociones", () =>
{
    try { return Results.Ok(new { status = "success", data = ReadJson<List<Promocion>>("promociones.json") }); }
    catch (Exception ex) { return Results.Problem(ex.Message); }
});

app.MapGet("/api/resenas", (int? producto_id) =>
{
    try
    {
        var resenas = ReadJson<List<Resena>>("resenas.json");
        if (producto_id.HasValue) resenas = resenas.Where(r => r.Producto_Id == producto_id.Value).ToList();
        return Results.Ok(new { status = "success", data = resenas });
    }
    catch (Exception ex) { return Results.Problem(ex.Message); }
});

app.Run();

record Producto(int Id, int Categoria_Id, string Nombre, decimal Precio, int Stock, string Descripcion, string Imagen);
record Categoria(int Id, string Nombre, string Icono);
record Promocion(int Id, string Titulo, string Descuento, string Codigo);
record Resena(int Id, int Producto_Id, string Usuario, string Comentario, int Estrellas);