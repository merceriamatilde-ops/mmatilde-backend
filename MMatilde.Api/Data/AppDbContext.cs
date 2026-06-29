using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Models;

namespace MMatilde.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<ConfiguracionSitio> ConfiguracionSitio { get; set; }
    public DbSet<Proveedor> Proveedores { get; set; }
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<Subcategoria> Subcategorias { get; set; }
    public DbSet<Marca> Marcas { get; set; }
    public DbSet<Color> Colores { get; set; }
    public DbSet<Producto> Productos { get; set; }
    public DbSet<ProductoVariante> ProductoVariantes { get; set; }
    public DbSet<ProductoImagen> ProductoImagenes { get; set; }
    public DbSet<ProductoRelacionado> ProductoRelacionados { get; set; }
    public DbSet<ReglaPrecio> ReglasPrecio { get; set; }
    public DbSet<SyncLog> SyncLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enums
        modelBuilder.HasPostgresEnum<RolUsuario>("rol_usuario");
        modelBuilder.HasPostgresEnum<EstadoSync>("estado_sync");
        modelBuilder.HasPostgresEnum<TipoPrecio>("tipo_precio");

        // Usuarios
        modelBuilder.Entity<Usuario>(b =>
        {
            b.ToTable("usuarios");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            b.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            b.HasIndex(e => e.Email).IsUnique();
            b.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(255).IsRequired();
            b.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            b.Property(e => e.Rol).HasColumnName("rol").HasConversion<string>();
            b.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            b.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        });

        // ConfiguracionSitio
        modelBuilder.Entity<ConfiguracionSitio>(b =>
        {
            b.ToTable("configuracion_sitio");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.Clave).HasColumnName("clave").HasMaxLength(100).IsRequired();
            b.HasIndex(e => e.Clave).IsUnique();
            b.Property(e => e.Valor).HasColumnName("valor").IsRequired();
            b.Property(e => e.Tipo).HasColumnName("tipo").HasMaxLength(20).HasDefaultValue("text");
            b.Property(e => e.Grupo).HasColumnName("grupo").HasMaxLength(50).IsRequired();
            b.Property(e => e.Label).HasColumnName("label").HasMaxLength(255).IsRequired();
            b.Property(e => e.Orden).HasColumnName("orden").HasDefaultValue(0);
            b.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        });

        // Proveedores
        modelBuilder.Entity<Proveedor>(b =>
        {
            b.ToTable("proveedores");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(255).IsRequired();
            b.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
            b.HasIndex(e => e.Slug).IsUnique();
            b.Property(e => e.UrlBase).HasColumnName("url_base").HasMaxLength(500);
            b.Property(e => e.ScrapingConfig).HasColumnName("scraping_config").HasColumnType("jsonb");
            b.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            b.Property(e => e.UltimaSync).HasColumnName("ultima_sync");
            b.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        });

        // Categorias
        modelBuilder.Entity<Categoria>(b =>
        {
            b.ToTable("categorias");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(255).IsRequired();
            b.HasIndex(e => e.Nombre).IsUnique();
            b.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
            b.HasIndex(e => e.Slug).IsUnique();
            b.Property(e => e.Descripcion).HasColumnName("descripcion");
            b.Property(e => e.Icono).HasColumnName("icono").HasMaxLength(50);
            b.Property(e => e.Imagen).HasColumnName("imagen").HasMaxLength(500);
            b.Property(e => e.Orden).HasColumnName("orden").HasDefaultValue(0);
            b.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            b.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        });

        // Subcategorias
        modelBuilder.Entity<Subcategoria>(b =>
        {
            b.ToTable("subcategorias");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(255).IsRequired();
            b.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
            b.Property(e => e.CategoriaId).HasColumnName("categoria_id");
            b.Property(e => e.Orden).HasColumnName("orden").HasDefaultValue(0);
            b.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            
            b.HasIndex(e => new { e.Slug, e.CategoriaId }).IsUnique();
            
            b.HasOne(e => e.Categoria)
             .WithMany(c => c.Subcategorias)
             .HasForeignKey(e => e.CategoriaId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Marcas
        modelBuilder.Entity<Marca>(b =>
        {
            b.ToTable("marcas");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(255).IsRequired();
            b.HasIndex(e => e.Nombre).IsUnique();
            b.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
            b.HasIndex(e => e.Slug).IsUnique();
            b.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        });

        // Colores
        modelBuilder.Entity<Color>(b =>
        {
            b.ToTable("colores");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
            b.HasIndex(e => e.Nombre).IsUnique();
            b.Property(e => e.CodigoHex).HasColumnName("codigo_hex").HasMaxLength(7);
            b.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
            b.HasIndex(e => e.Slug).IsUnique();
            b.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        });

        // Productos
        modelBuilder.Entity<Producto>(b =>
        {
            b.ToTable("productos");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.CodigoMakor).HasColumnName("codigo_makor").HasMaxLength(50).IsRequired();
            b.HasIndex(e => e.CodigoMakor).IsUnique();
            b.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(500).IsRequired();
            b.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(550).IsRequired();
            b.HasIndex(e => e.Slug).IsUnique();
            b.Property(e => e.Descripcion).HasColumnName("descripcion");
            b.Property(e => e.Composicion).HasColumnName("composicion").HasMaxLength(500);
            b.Property(e => e.PrecioMayorista).HasColumnName("precio_mayorista").HasColumnType("numeric(18,2)");
            b.Property(e => e.PrecioMinorista).HasColumnName("precio_minorista").HasColumnType("numeric(18,2)");
            b.Property(e => e.DescuentoPorcentaje).HasColumnName("descuento_porcentaje").HasColumnType("numeric(5,2)").HasDefaultValue(0);
            b.Property(e => e.Destacado).HasColumnName("destacado").HasDefaultValue(false);
            b.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(false);
            b.Property(e => e.CategoriaId).HasColumnName("categoria_id");
            b.Property(e => e.SubcategoriaId).HasColumnName("subcategoria_id");
            b.Property(e => e.MarcaId).HasColumnName("marca_id");
            b.Property(e => e.ProveedorId).HasColumnName("proveedor_id");
            b.Property(e => e.UltimaSync).HasColumnName("ultima_sync");
            b.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            b.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

            b.HasIndex(e => e.CategoriaId);
            b.HasIndex(e => e.Activo);
            b.HasIndex(e => e.ProveedorId);
            b.HasIndex(e => e.Destacado);

            b.HasOne(e => e.Categoria)
             .WithMany(c => c.Productos)
             .HasForeignKey(e => e.CategoriaId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Subcategoria)
             .WithMany(s => s.Productos)
             .HasForeignKey(e => e.SubcategoriaId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Marca)
             .WithMany()
             .HasForeignKey(e => e.MarcaId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Proveedor)
             .WithMany(p => p.Productos)
             .HasForeignKey(e => e.ProveedorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ProductoVariantes
        modelBuilder.Entity<ProductoVariante>(b =>
        {
            b.ToTable("producto_variantes");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.ProductoId).HasColumnName("producto_id");
            b.Property(e => e.ColorId).HasColumnName("color_id");
            b.Property(e => e.Talle).HasColumnName("talle").HasMaxLength(50);
            b.Property(e => e.Medida).HasColumnName("medida").HasMaxLength(100);
            b.Property(e => e.CodigoArticulo).HasColumnName("codigo_articulo").HasMaxLength(100);
            b.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(false);
            b.Property(e => e.Orden).HasColumnName("orden").HasDefaultValue(0);
            b.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            b.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

            b.HasIndex(e => new { e.ProductoId, e.ColorId, e.Talle, e.Medida }).IsUnique();
            b.HasIndex(e => e.ProductoId);
            b.HasIndex(e => e.Activo);

            b.HasOne(e => e.Producto)
             .WithMany(p => p.Variantes)
             .HasForeignKey(e => e.ProductoId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(e => e.Color)
             .WithMany()
             .HasForeignKey(e => e.ColorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ProductoImagenes
        modelBuilder.Entity<ProductoImagen>(b =>
        {
            b.ToTable("producto_imagenes");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.ProductoId).HasColumnName("producto_id");
            b.Property(e => e.VarianteId).HasColumnName("variante_id");
            b.Property(e => e.CloudinaryPublicId).HasColumnName("cloudinary_public_id").HasMaxLength(500);
            b.Property(e => e.UrlOriginal).HasColumnName("url_original").HasMaxLength(1000);
            b.Property(e => e.AltText).HasColumnName("alt_text").HasMaxLength(300);
            b.Property(e => e.Orden).HasColumnName("orden").HasDefaultValue(0);
            b.Property(e => e.EsPrincipal).HasColumnName("es_principal").HasDefaultValue(false);
            b.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

            b.HasOne(e => e.Producto)
             .WithMany(p => p.Imagenes)
             .HasForeignKey(e => e.ProductoId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(e => e.Variante)
             .WithMany(v => v.Imagenes)
             .HasForeignKey(e => e.VarianteId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ReglasPrecio
        modelBuilder.Entity<ReglaPrecio>(b =>
        {
            b.ToTable("reglas_precio");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.CategoriaId).HasColumnName("categoria_id");
            b.Property(e => e.SubcategoriaId).HasColumnName("subcategoria_id");
            b.Property(e => e.MarcaId).HasColumnName("marca_id");
            b.Property(e => e.MargenPorcentaje).HasColumnName("margen_porcentaje").HasColumnType("numeric(5,2)").IsRequired();
            b.Property(e => e.Tipo).HasColumnName("tipo").HasConversion<string>().IsRequired();
            b.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            b.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            b.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        });

        // SyncLogs
        modelBuilder.Entity<SyncLog>(b =>
        {
            b.ToTable("sync_logs");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.ProveedorId).HasColumnName("proveedor_id");
            b.Property(e => e.Estado).HasColumnName("estado").HasConversion<string>().HasDefaultValue(EstadoSync.PENDIENTE);
            b.Property(e => e.ProductosNuevos).HasColumnName("productos_nuevos").HasDefaultValue(0);
            b.Property(e => e.ProductosActualizados).HasColumnName("productos_actualizados").HasDefaultValue(0);
            b.Property(e => e.Errores).HasColumnName("errores").HasDefaultValue(0);
            b.Property(e => e.DetalleErrores).HasColumnName("detalle_errores").HasColumnType("jsonb");
            b.Property(e => e.IniciadoEn).HasColumnName("iniciado_en").HasDefaultValueSql("now()");
            b.Property(e => e.FinalizadoEn).HasColumnName("finalizado_en");

            b.HasOne(e => e.Proveedor)
             .WithMany(p => p.SyncLogs)
             .HasForeignKey(e => e.ProveedorId)
             .OnDelete(DeleteBehavior.Restrict);
        });
        // ProductoRelacionado
        modelBuilder.Entity<ProductoRelacionado>(b =>
        {
            b.ToTable("producto_relacionados");
            b.HasKey(e => new { e.ProductoPrincipalId, e.ProductoVinculadoId });
            
            b.Property(e => e.ProductoPrincipalId).HasColumnName("producto_principal_id");
            b.Property(e => e.ProductoVinculadoId).HasColumnName("producto_vinculado_id");

            b.HasOne(e => e.ProductoPrincipal)
             .WithMany(p => p.Relacionados)
             .HasForeignKey(e => e.ProductoPrincipalId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(e => e.ProductoVinculado)
             .WithMany()
             .HasForeignKey(e => e.ProductoVinculadoId)
             .OnDelete(DeleteBehavior.Restrict); // Prevent circular cascade
        });
    }
}
