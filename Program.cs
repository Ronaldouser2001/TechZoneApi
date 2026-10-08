using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configurar el puerto para Render
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

// Habilitar CORS para que el Frontend se conecte
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();
app.UseCors("AllowAll");

// Helper para leer los archivos JSON
T ReadJson<T>(string fileName)
{
    var path = Path.Combine(Directory.GetCurrentDirectory(), "Data", fileName);
    var json = File.ReadAllText(path);
    return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
}

// Endpoints de la API
app.MapGet("/api/productos", (int? categoria_id, string? search) =>
{
    var productos = ReadJson<List<Producto>>("productos.json");
    var query = productos.AsEnumerable();
    if (categoria_id.HasValue) query = query.Where(p => p.Categoria_Id == categoria_id.Value);
    if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.Nombre.Contains(search, StringComparison.OrdinalIgnoreCase));
    var resultado = query.ToList();
    return Results.Ok(new { status = "success", count = resultado.Count, data = resultado });
});

app.MapGet("/api/productos/{id:int}", (int id) =>
{
    var productos = ReadJson<List<Producto>>("productos.json");
    var producto = productos.FirstOrDefault(p => p.Id == id);
    return producto is not null ? Results.Ok(new { status = "success", data = producto }) : Results.NotFound(new { status = "error", message = "Producto no encontrado" });
});

app.MapGet("/api/categorias", () => Results.Ok(new { status = "success", data = ReadJson<List<Categoria>>("categorias.json") }));
app.MapGet("/api/promociones", () => Results.Ok(new { status = "success", data = ReadJson<List<Promocion>>("promociones.json") }));
app.MapGet("/api/resenas", (int? producto_id) =>
{
    var resenas = ReadJson<List<Resena>>("resenas.json");
    if (producto_id.HasValue) resenas = resenas.Where(r => r.Producto_Id == producto_id.Value).ToList();
    return Results.Ok(new { status = "success", data = resenas });
});

app.Run();

// Modelos de datos
record Producto(int Id, int Categoria_Id, string Nombre, decimal Precio, int Stock, string Descripcion, string Imagen);
record Categoria(int Id, string Nombre, string Icono);
record Promocion(int Id, string Titulo, string Descuento, string Codigo);
record Resena(int Id, int Producto_Id, string Usuario, string Comentario, int Estrellas);