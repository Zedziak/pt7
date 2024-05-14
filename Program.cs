using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Threading;

public class Program
{
    public class Engine
    {
        public double Displacement { get; set; }
        public int HorsePower { get; set; }
        public string Model { get; set; }

        public Engine(double displacement, int horsePower, string model)
        {
            Displacement = displacement;
            HorsePower = horsePower;
            Model = model;
        }
    }

    public class Car
    {
        public string Model { get; set; }
        public Engine Motor { get; set; }
        public int Year { get; set; }

        public Car(string model, Engine motor, int year)
        {
            Model = model;
            Motor = motor;
            Year = year;
        }
    }

    public static void Main(string[] args)
    {
        List<Car> myCars = new List<Car>()
        {
            new Car("E250", new Engine(1.8, 204, "CGI"), 2009),
            new Car("E350", new Engine(3.5, 292, "CGI"), 2009),
            new Car("A6", new Engine(2.5, 187, "FSI"), 2012),
            new Car("A6", new Engine(2.8, 220, "FSI"), 2012),
            new Car("A6", new Engine(3.0, 295, "TFSI"), 2012),
            new Car("A6", new Engine(2.0, 175, "TDI"), 2011),
            new Car("A6", new Engine(3.0, 309, "TDI"), 2011),
            new Car("S6", new Engine(4.0, 414, "TFSI"), 2012),
            new Car("S8", new Engine(4.0, 513, "TFSI"), 2012)
        };

        var query1 = myCars.Where(c => c.Model == "A6")
                           .Select(c => new
                           {
                               engineType = c.Motor.Model == "TDI" ? "diesel" : "petrol",
                               hppl = (double)c.Motor.HorsePower / c.Motor.Displacement
                           });

        var groupedByEngineType = query1.GroupBy(q => q.engineType);
        foreach (var group in groupedByEngineType)
        {
            Console.WriteLine($"{group.Key}: {group.Average(g => g.hppl)}");
        }

        SerializeToXml(myCars, "CarsCollection.xml");
        var deserializedCars = DeserializeFromXml("CarsCollection.xml");

        XElement rootNode = XElement.Load("CarsCollection.xml");
        double sumHP = (double)rootNode.XPathEvaluate("sum(//*[local-name()='car'][*[local-name()='engine']/@model != 'TDI']/*[local-name()='engine']/*[local-name()='horsePower']/text())");
        double countCars = (double)rootNode.XPathEvaluate("count(//*[local-name()='car'][*[local-name()='engine']/@model != 'TDI'])");
        double avgHP = sumHP / countCars;
        Console.WriteLine($"Średnia moc samochodów o silnikach innych niż TDI: {avgHP}");

        var uniqueModels = rootNode.XPathSelectElements("//car")
        .GroupBy(c => c.Element("model").Value.Trim())
        .Where(g => g.Count() == 1)
        .Select(g => g.Key);

        Console.WriteLine("Modele występujące tylko raz:");
        foreach (var model in uniqueModels)
        {
            Console.WriteLine(model);
        }

        Thread.Sleep(10000);

        createXmlFromLinq(myCars);

        GenerateXhtmlTable(myCars);

        ModifyXmlDocument("CarsCollection.xml");
    }

    private static void SerializeToXml(List<Car> cars, string filename)
    {
        XElement carsXml = new XElement("cars",
            cars.Select(c =>
                new XElement("car",
                    new XElement("model", c.Model),
                    new XElement("engine",
                        new XAttribute("model", c.Motor.Model),
                        new XElement("horsePower", c.Motor.HorsePower),
                        new XElement("displacement", c.Motor.Displacement)
                    ),
                    new XElement("year", c.Year)
                )
            )
        );
        carsXml.Save(filename);
    }

    private static List<Car> DeserializeFromXml(string filename)
    {
        XDocument doc = XDocument.Load(filename);
        List<Car> cars = new List<Car>();

        foreach (var carElement in doc.Root.Elements("car"))
        {
            string model = carElement.Element("model").Value;
            string engineModel = carElement.Element("engine").Attribute("model").Value;
            double displacement;
            double.TryParse(carElement.Element("engine").Element("displacement").Value, NumberStyles.Any, CultureInfo.InvariantCulture, out displacement); // Zmieniono na TryParse z uwzględnieniem kultury InvariantCulture
            int horsePower;
            int.TryParse(carElement.Element("engine").Element("horsePower").Value, out horsePower);
            int year = int.Parse(carElement.Element("year").Value);

            Engine engine = new Engine(displacement, horsePower, engineModel);
            Car car = new Car(model, engine, year);
            cars.Add(car);
        }

        return cars;
    }

    private static void createXmlFromLinq(List<Car> myCars)
    {
        IEnumerable<XElement> nodes = myCars.Select(c =>
            new XElement("car",
                new XElement("model", c.Model),
                new XElement("engine",
                    new XAttribute("model", c.Motor.Model),
                    new XElement("horsePower", c.Motor.HorsePower),
                    new XElement("displacement", c.Motor.Displacement)
                ),
                new XElement("year", c.Year)
            )
        );
        XElement rootNode = new XElement("cars", nodes);
        rootNode.Save("CarsFromLinq.xml");
    }

    private static void GenerateXhtmlTable(List<Car> myCars)
    {
        XElement table = new XElement("table",
            new XElement("tr",
                new XElement("th", "Model"),
                new XElement("th", "Engine Model"),
                new XElement("th", "Horse Power"),
                new XElement("th", "Displacement"),
                new XElement("th", "Year")
            ),
            myCars.Select(c =>
                new XElement("tr",
                    new XElement("td", c.Model),
                    new XElement("td", c.Motor.Model),
                    new XElement("td", c.Motor.HorsePower),
                    new XElement("td", c.Motor.Displacement),
                    new XElement("td", c.Year)
                )
            )
        );

        XDocument xhtmlDoc = new XDocument(
            new XDocumentType("html", null, null, null),
            new XElement("html",
                new XElement("head"),
                new XElement("body", table)
            )
        );

        xhtmlDoc.Save("CarsTable.html");
    }

    private static void ModifyXmlDocument(string filename)
    {
        XDocument doc = XDocument.Load(filename);
        foreach (var engineNode in doc.Descendants("engine"))
        {
            engineNode.Element("horsePower").Name = "hp";
            engineNode.Parent.Element("year").Add(new XAttribute("year", engineNode.Parent.Element("model").Value));
            engineNode.Parent.Element("year").Name = "model";
        }
        doc.Save(filename);
    }
}
