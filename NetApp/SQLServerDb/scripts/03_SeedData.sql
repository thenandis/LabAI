/* =============================================================================
   03_SeedData.sql
   Seed data from Microsoft's Northwind sample database
   (8 categories, 29 suppliers, 77 products). Only runs when tables are empty.
   ============================================================================= */
USE NorthwindCatalog;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

/* ---------- Categories ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Categories)
BEGIN
    SET IDENTITY_INSERT dbo.Categories ON;
    INSERT INTO dbo.Categories (CategoryID, CategoryName, Description) VALUES
    (1, N'Beverages',      N'Soft drinks, coffees, teas, beers, and ales'),
    (2, N'Condiments',     N'Sweet and savory sauces, relishes, spreads, and seasonings'),
    (3, N'Confections',    N'Desserts, candies, and sweet breads'),
    (4, N'Dairy Products', N'Cheeses'),
    (5, N'Grains/Cereals', N'Breads, crackers, pasta, and cereal'),
    (6, N'Meat/Poultry',   N'Prepared meats'),
    (7, N'Produce',        N'Dried fruit and bean curd'),
    (8, N'Seafood',        N'Seaweed and fish');
    SET IDENTITY_INSERT dbo.Categories OFF;
END

/* ---------- Suppliers ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Suppliers)
BEGIN
    SET IDENTITY_INSERT dbo.Suppliers ON;
    INSERT INTO dbo.Suppliers (SupplierID, CompanyName, ContactName, City, Country, Phone) VALUES
    (1,  N'Exotic Liquids',                          N'Charlotte Cooper',          N'London',        N'UK',          N'(171) 555-2222'),
    (2,  N'New Orleans Cajun Delights',              N'Shelley Burke',             N'New Orleans',   N'USA',         N'(100) 555-4822'),
    (3,  N'Grandma Kelly''s Homestead',              N'Regina Murphy',             N'Ann Arbor',     N'USA',         N'(313) 555-5735'),
    (4,  N'Tokyo Traders',                           N'Yoshi Nagase',              N'Tokyo',         N'Japan',       N'(03) 3555-5011'),
    (5,  N'Cooperativa de Quesos ''Las Cabras''',    N'Antonio del Valle Saavedra',N'Oviedo',        N'Spain',       N'(98) 598 76 54'),
    (6,  N'Mayumi''s',                               N'Mayumi Ohno',               N'Osaka',         N'Japan',       N'(06) 431-7877'),
    (7,  N'Pavlova, Ltd.',                           N'Ian Devling',               N'Melbourne',     N'Australia',   N'(03) 444-2343'),
    (8,  N'Specialty Biscuits, Ltd.',                N'Peter Wilson',              N'Manchester',    N'UK',          N'(161) 555-4448'),
    (9,  N'PB Knäckebröd AB',                        N'Lars Peterson',             N'Göteborg',      N'Sweden',      N'031-987 65 43'),
    (10, N'Refrescos Americanas LTDA',               N'Carlos Diaz',               N'Sao Paulo',     N'Brazil',      N'(11) 555 4640'),
    (11, N'Heli Süßwaren GmbH & Co. KG',             N'Petra Winkler',             N'Berlin',        N'Germany',     N'(010) 9984510'),
    (12, N'Plutzer Lebensmittelgroßmärkte AG',       N'Martin Bein',               N'Frankfurt',     N'Germany',     N'(069) 992755'),
    (13, N'Nord-Ost-Fisch Handelsgesellschaft mbH',  N'Sven Petersen',             N'Cuxhaven',      N'Germany',     N'(04721) 8713'),
    (14, N'Formaggi Fortini s.r.l.',                 N'Elio Rossi',                N'Ravenna',       N'Italy',       N'(0544) 60323'),
    (15, N'Norske Meierier',                         N'Beate Vileid',              N'Sandvika',      N'Norway',      N'(0)2-953010'),
    (16, N'Bigfoot Breweries',                       N'Cheryl Saylor',             N'Bend',          N'USA',         N'(503) 555-9931'),
    (17, N'Svensk Sjöföda AB',                       N'Michael Björn',             N'Stockholm',     N'Sweden',      N'08-123 45 67'),
    (18, N'Aux joyeux ecclésiastiques',              N'Guylène Nodier',            N'Paris',         N'France',      N'(1) 03.83.00.68'),
    (19, N'New England Seafood Cannery',             N'Robb Merchant',             N'Boston',        N'USA',         N'(617) 555-3267'),
    (20, N'Leka Trading',                            N'Chandra Leka',              N'Singapore',     N'Singapore',   N'555-8787'),
    (21, N'Lyngbysild',                              N'Niels Petersen',            N'Lyngby',        N'Denmark',     N'43844108'),
    (22, N'Zaanse Snoepfabriek',                     N'Dirk Luchte',               N'Zaandam',       N'Netherlands', N'(12345) 1212'),
    (23, N'Karkki Oy',                               N'Anne Heikkonen',            N'Lappeenranta',  N'Finland',     N'(953) 10956'),
    (24, N'G''day, Mate',                            N'Wendy Mackenzie',           N'Sydney',        N'Australia',   N'(02) 555-5914'),
    (25, N'Ma Maison',                               N'Jean-Guy Lauzon',           N'Montréal',      N'Canada',      N'(514) 555-9022'),
    (26, N'Pasta Buttini s.r.l.',                    N'Giovanni Giudici',          N'Salerno',       N'Italy',       N'(089) 6547665'),
    (27, N'Escargots Nouveaux',                      N'Marie Delamare',            N'Montceau',      N'France',      N'85.57.00.07'),
    (28, N'Gai pâturage',                            N'Eliane Noz',                N'Annecy',        N'France',      N'38.76.98.06'),
    (29, N'Forêts d''érables',                       N'Chantal Goulet',            N'Ste-Hyacinthe', N'Canada',      N'(514) 555-2955');
    SET IDENTITY_INSERT dbo.Suppliers OFF;
END

/* ---------- Products ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    SET IDENTITY_INSERT dbo.Products ON;
    INSERT INTO dbo.Products
        (ProductID, ProductName, SupplierID, CategoryID, QuantityPerUnit, UnitPrice, UnitsInStock, UnitsOnOrder, ReorderLevel, Discontinued)
    VALUES
    (1,  N'Chai',                             1,  1, N'10 boxes x 20 bags',    18.00,  39,   0, 10, 0),
    (2,  N'Chang',                            1,  1, N'24 - 12 oz bottles',    19.00,  17,  40, 25, 0),
    (3,  N'Aniseed Syrup',                    1,  2, N'12 - 550 ml bottles',   10.00,  13,  70, 25, 0),
    (4,  N'Chef Anton''s Cajun Seasoning',    2,  2, N'48 - 6 oz jars',        22.00,  53,   0,  0, 0),
    (5,  N'Chef Anton''s Gumbo Mix',          2,  2, N'36 boxes',              21.35,   0,   0,  0, 1),
    (6,  N'Grandma''s Boysenberry Spread',    3,  2, N'12 - 8 oz jars',        25.00, 120,   0, 25, 0),
    (7,  N'Uncle Bob''s Organic Dried Pears', 3,  7, N'12 - 1 lb pkgs.',       30.00,  15,   0, 10, 0),
    (8,  N'Northwoods Cranberry Sauce',       3,  2, N'12 - 12 oz jars',       40.00,   6,   0,  0, 0),
    (9,  N'Mishi Kobe Niku',                  4,  6, N'18 - 500 g pkgs.',      97.00,  29,   0,  0, 1),
    (10, N'Ikura',                            4,  8, N'12 - 200 ml jars',      31.00,  31,   0,  0, 0),
    (11, N'Queso Cabrales',                   5,  4, N'1 kg pkg.',             21.00,  22,  30, 30, 0),
    (12, N'Queso Manchego La Pastora',        5,  4, N'10 - 500 g pkgs.',      38.00,  86,   0,  0, 0),
    (13, N'Konbu',                            6,  8, N'2 kg box',               6.00,  24,   0,  5, 0),
    (14, N'Tofu',                             6,  7, N'40 - 100 g pkgs.',      23.25,  35,   0,  0, 0),
    (15, N'Genen Shouyu',                     6,  2, N'24 - 250 ml bottles',   15.50,  39,   0,  5, 0),
    (16, N'Pavlova',                          7,  3, N'32 - 500 g boxes',      17.45,  29,   0, 10, 0),
    (17, N'Alice Mutton',                     7,  6, N'20 - 1 kg tins',        39.00,   0,   0,  0, 1),
    (18, N'Carnarvon Tigers',                 7,  8, N'16 kg pkg.',            62.50,  42,   0,  0, 0),
    (19, N'Teatime Chocolate Biscuits',       8,  3, N'10 boxes x 12 pieces',   9.20,  25,   0,  5, 0),
    (20, N'Sir Rodney''s Marmalade',          8,  3, N'30 gift boxes',         81.00,  40,   0,  0, 0),
    (21, N'Sir Rodney''s Scones',             8,  3, N'24 pkgs. x 4 pieces',   10.00,   3,  40,  5, 0),
    (22, N'Gustaf''s Knäckebröd',             9,  5, N'24 - 500 g pkgs.',      21.00, 104,   0, 25, 0),
    (23, N'Tunnbröd',                         9,  5, N'12 - 250 g pkgs.',       9.00,  61,   0, 25, 0),
    (24, N'Guaraná Fantástica',               10, 1, N'12 - 355 ml cans',       4.50,  20,   0,  0, 1),
    (25, N'NuNuCa Nuß-Nougat-Creme',          11, 3, N'20 - 450 g glasses',    14.00,  76,   0, 30, 0),
    (26, N'Gumbär Gummibärchen',              11, 3, N'100 - 250 g bags',      31.23,  15,   0,  0, 0),
    (27, N'Schoggi Schokolade',               11, 3, N'100 - 100 g pieces',    43.90,  49,   0, 30, 0),
    (28, N'Rössle Sauerkraut',                12, 7, N'25 - 825 g cans',       45.60,  26,   0,  0, 1),
    (29, N'Thüringer Rostbratwurst',          12, 6, N'50 bags x 30 sausgs.', 123.79,   0,   0,  0, 1),
    (30, N'Nord-Ost Matjeshering',            13, 8, N'10 - 200 g glasses',    25.89,  10,   0, 15, 0),
    (31, N'Gorgonzola Telino',                14, 4, N'12 - 100 g pkgs',       12.50,   0,  70, 20, 0),
    (32, N'Mascarpone Fabioli',               14, 4, N'24 - 200 g pkgs.',      32.00,   9,  40, 25, 0),
    (33, N'Geitost',                          15, 4, N'500 g',                  2.50, 112,   0, 20, 0),
    (34, N'Sasquatch Ale',                    16, 1, N'24 - 12 oz bottles',    14.00, 111,   0, 15, 0),
    (35, N'Steeleye Stout',                   16, 1, N'24 - 12 oz bottles',    18.00,  20,   0, 15, 0),
    (36, N'Inlagd Sill',                      17, 8, N'24 - 250 g jars',       19.00, 112,   0, 20, 0),
    (37, N'Gravad lax',                       17, 8, N'12 - 500 g pkgs.',      26.00,  11,  50, 25, 0),
    (38, N'Côte de Blaye',                    18, 1, N'12 - 75 cl bottles',   263.50,  17,   0, 15, 0),
    (39, N'Chartreuse verte',                 18, 1, N'750 cc per bottle',     18.00,  69,   0,  5, 0),
    (40, N'Boston Crab Meat',                 19, 8, N'24 - 4 oz tins',        18.40, 123,   0, 30, 0),
    (41, N'Jack''s New England Clam Chowder', 19, 8, N'12 - 12 oz cans',        9.65,  85,   0, 10, 0),
    (42, N'Singaporean Hokkien Fried Mee',    20, 5, N'32 - 1 kg pkgs.',       14.00,  26,   0,  0, 1),
    (43, N'Ipoh Coffee',                      20, 1, N'16 - 500 g tins',       46.00,  17,  10, 25, 0),
    (44, N'Gula Malacca',                     20, 2, N'20 - 2 kg bags',        19.45,  27,   0, 15, 0),
    (45, N'Rogede sild',                      21, 8, N'1k pkg.',                9.50,   5,  70, 15, 0),
    (46, N'Spegesild',                        21, 8, N'4 - 450 g glasses',     12.00,  95,   0,  0, 0),
    (47, N'Zaanse koeken',                    22, 3, N'10 - 4 oz boxes',        9.50,  36,   0,  0, 0),
    (48, N'Chocolade',                        22, 3, N'10 pkgs.',              12.75,  15,  70, 25, 0),
    (49, N'Maxilaku',                         23, 3, N'24 - 50 g pkgs.',       20.00,  10,  60, 15, 0),
    (50, N'Valkoinen suklaa',                 23, 3, N'12 - 100 g bars',       16.25,  65,   0, 30, 0),
    (51, N'Manjimup Dried Apples',            24, 7, N'50 - 300 g pkgs.',      53.00,  20,   0, 10, 0),
    (52, N'Filo Mix',                         24, 5, N'16 - 2 kg boxes',        7.00,  38,   0, 25, 0),
    (53, N'Perth Pasties',                    24, 6, N'48 pieces',             32.80,   0,   0,  0, 1),
    (54, N'Tourtière',                        25, 6, N'16 pies',                7.45,  21,   0, 10, 0),
    (55, N'Pâté chinois',                     25, 6, N'24 boxes x 2 pies',     24.00, 115,   0, 20, 0),
    (56, N'Gnocchi di nonna Alice',           26, 5, N'24 - 250 g pkgs.',      38.00,  21,  10, 30, 0),
    (57, N'Ravioli Angelo',                   26, 5, N'24 - 250 g pkgs.',      19.50,  36,   0, 20, 0),
    (58, N'Escargots de Bourgogne',           27, 8, N'24 pieces',             13.25,  62,   0, 20, 0),
    (59, N'Raclette Courdavault',             28, 4, N'5 kg pkg.',             55.00,  79,   0,  0, 0),
    (60, N'Camembert Pierrot',                28, 4, N'15 - 300 g rounds',     34.00,  19,   0,  0, 0),
    (61, N'Sirop d''érable',                  29, 2, N'24 - 500 ml bottles',   28.50, 113,   0, 25, 0),
    (62, N'Tarte au sucre',                   29, 3, N'48 pies',               49.30,  17,   0,  0, 0),
    (63, N'Vegie-spread',                     7,  2, N'15 - 625 g jars',       43.90,  24,   0,  5, 0),
    (64, N'Wimmers gute Semmelknödel',        12, 5, N'20 bags x 4 pieces',    33.25,  22,  80, 30, 0),
    (65, N'Louisiana Fiery Hot Pepper Sauce', 2,  2, N'32 - 8 oz bottles',     21.05,  76,   0,  0, 0),
    (66, N'Louisiana Hot Spiced Okra',        2,  2, N'24 - 8 oz jars',        17.00,   4, 100, 20, 0),
    (67, N'Laughing Lumberjack Lager',        16, 1, N'24 - 12 oz bottles',    14.00,  52,   0, 10, 0),
    (68, N'Scottish Longbreads',              8,  3, N'10 boxes x 8 pieces',   12.50,   6,  10, 15, 0),
    (69, N'Gudbrandsdalsost',                 15, 4, N'10 kg pkg.',            36.00,  26,   0, 15, 0),
    (70, N'Outback Lager',                    7,  1, N'24 - 355 ml bottles',   15.00,  15,  10, 30, 0),
    (71, N'Flotemysost',                      15, 4, N'10 - 500 g pkgs.',      21.50,  26,   0,  0, 0),
    (72, N'Mozzarella di Giovanni',           14, 4, N'24 - 200 g pkgs.',      34.80,  14,   0,  0, 0),
    (73, N'Röd Kaviar',                       17, 8, N'24 - 150 g jars',       15.00, 101,   0,  5, 0),
    (74, N'Longlife Tofu',                    4,  7, N'5 kg pkg.',             10.00,   4,  20,  5, 0),
    (75, N'Rhönbräu Klosterbier',             12, 1, N'24 - 0.5 l bottles',     7.75, 125,   0, 25, 0),
    (76, N'Lakkalikööri',                     23, 1, N'500 ml',                18.00,  57,   0, 20, 0),
    (77, N'Original Frankfurter grüne Soße',  12, 2, N'12 boxes',              13.00,  32,   0, 15, 0);
    SET IDENTITY_INSERT dbo.Products OFF;
END

COMMIT TRANSACTION;
GO

PRINT 'Seed complete.';
SELECT (SELECT COUNT(*) FROM dbo.Categories) AS Categories,
       (SELECT COUNT(*) FROM dbo.Suppliers)  AS Suppliers,
       (SELECT COUNT(*) FROM dbo.Products)   AS Products;
GO
