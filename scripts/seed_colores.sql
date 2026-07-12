-- Seed idempotente de colores.
-- Inserta solo los que NO existan (por nombre case-insensitive o por slug).
-- No pisa colores ya cargados (respeta ediciones manuales de hex).
-- Seguro de correr varias veces y en desa/prod.

INSERT INTO colores (nombre, codigo_hex, slug)
SELECT v.nombre, v.codigo_hex, v.slug
FROM (VALUES
    ('Blanco',                  '#FFFFFF', 'blanco'),
    ('Gris Perla',              '#C0C5C8', 'gris-perla'),
    ('Gris Medio',              '#8E9396', 'gris-medio'),
    ('Gris Topo',               '#66696C', 'gris-topo'),
    ('Negro',                   '#1A1A1A', 'negro'),
    ('Celeste Bebé',            '#A9D5ED', 'celeste-bebe'),
    ('Celeste Pastel',          '#6097CE', 'celeste-pastel'),
    ('Turquesa',                '#007AAB', 'turquesa'),
    ('Azul Francia',            '#22529A', 'azul-francia'),

    ('Rojo',                    '#E42A25', 'rojo'),
    ('Bordó',                   '#641F2B', 'bordo'),
    ('Amarillo Patito',         '#FEF9B6', 'amarillo-patito'),
    ('Amarillo Oro',            '#FBA044', 'amarillo-oro'),
    ('Naranja',                 '#FD5634', 'naranja'),
    ('Natural',                 '#FFF8EA', 'natural'),
    ('Beige',                   '#F5CDA8', 'beige'),
    ('Habano',                  '#7B4C38', 'habano'),
    ('Marrón Claro',            '#773B28', 'marron-claro'),

    ('Verde Oscuro',            '#1F3628', 'verde-oscuro'),
    ('Verde Militar',           '#4B4F3B', 'verde-militar'),
    ('Verde Agua',              '#C6F4EA', 'verde-agua'),
    ('Mostaza',                 '#E5973A', 'mostaza'),
    ('Violeta',                 '#52357A', 'violeta'),
    ('Salmón',                  '#FFB39E', 'salmon'),
    ('Rosa Dior',               '#F98FB5', 'rosa-dior'),
    ('Rosa Cristal',            '#FDE8F1', 'rosa-cristal'),
    ('Celeste Claro',           '#CBEAEF', 'celeste-claro'),

    ('Yute',                    '#BBA999', 'yute'),
    ('Pedrejón',                '#E2CEB4', 'pedrejon'),
    ('Arena',                   '#EFE0CE', 'arena'),
    ('Gris Pluma',              '#C5BAAD', 'gris-pluma'),
    ('L. Aceitunado',           '#9B8C7A', 'l-aceitunado'),
    ('Verde Secreto',           '#96937C', 'verde-secreto'),
    ('Nuez',                    '#786C5F', 'nuez'),
    ('Beige Gamo',              '#DEBA9F', 'beige-gamo'),
    ('Violeta Pastel',          '#D0C3EA', 'violeta-pastel'),

    ('Coral',                   '#FF5E74', 'coral'),
    ('A. Indigo',               '#324C53', 'a-indigo'),
    ('A. Navy',                 '#151D28', 'a-navy'),
    ('Verde Atlantis',          '#42C79E', 'verde-atlantis'),
    ('Maíz',                    '#FED192', 'maiz'),
    ('Naranja de Jaffa',        '#FF6A39', 'naranja-de-jaffa'),
    ('Ocre Quemado',            '#D85237', 'ocre-quemado'),
    ('Petróleo',                '#215551', 'petroleo'),
    ('Fresa',                   '#FFAED2', 'fresa'),

    ('Teja',                    '#8A2D3A', 'teja'),
    ('Hortensia',               '#5E3A60', 'hortensia'),
    ('Turquesa Claro',          '#1EC9E8', 'turquesa-claro'),
    ('Rosa Plateado',           '#F7B5BA', 'rosa-plateado'),
    ('Sandía',                  '#FF3B5D', 'sandia'),
    ('Suela (texturado)',       '#A05F1D', 'suela-texturado'),
    ('Mandarina',               '#FF7B39', 'mandarina'),
    ('Rosa Viejo (texturado)',  '#8E4964', 'rosa-viejo-texturado')
) AS v(nombre, codigo_hex, slug)
WHERE NOT EXISTS (
    SELECT 1 FROM colores c
    WHERE lower(c.nombre) = lower(v.nombre)
       OR c.slug = v.slug
);
