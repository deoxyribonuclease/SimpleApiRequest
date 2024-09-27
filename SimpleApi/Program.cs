using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class NationalizeResponse
{
    public string Name { get; set; }
    public List<CountryProbability> Country { get; set; }
}

public class CountryProbability
{
    public string Country_Id { get; set; }
    public double Probability { get; set; }
}

class Program
{
    private const string ConnectionString = "Data Source=nationalize.db;Version=3;";

    static async Task Main(string[] args)
    {
        using var connection = new SQLiteConnection(ConnectionString);
        connection.Open();
        CreateDatabase(connection);
        string choice = "";
        while (choice != "4")
        {
            Console.WriteLine("1 - Fetch nationalities\n2 - Read saved data\n3 - Clear saved data\n4 - Exit");
            choice = Console.ReadLine();
            switch (choice)
            {
                case "1": await FetchAndStoreNationalities(connection); break;
                case "2": ReadNationalities(connection); break;
                case "3": ClearNationalities(connection); break;
            }
        }
    }

    private static async Task FetchAndStoreNationalities(SQLiteConnection connection)
    {
        Console.WriteLine("Enter a name:");
        string name = Console.ReadLine();
        using var client = new HttpClient();
        var response = await client.GetStringAsync($"https://api.nationalize.io/?name={name}");
        var nationalizeData = JsonConvert.DeserializeObject<NationalizeResponse>(response);
        await SaveNationalitiesToDatabaseAsync(nationalizeData, connection);
    }

    private static async Task SaveNationalitiesToDatabaseAsync(NationalizeResponse nationalizeData, SQLiteConnection connection)
    {
        var checkQuery = "SELECT COUNT(1) FROM Nationalities WHERE Name = @name AND Country_Id = @countryId";
        var insertQuery = "INSERT INTO Nationalities (Name, Country_Id, Probability) VALUES (@name, @countryId, @probability)";

        foreach (var country in nationalizeData.Country)
        {
            using var checkCommand = new SQLiteCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@name", nationalizeData.Name);
            checkCommand.Parameters.AddWithValue("@countryId", country.Country_Id);

            var exists = (long)await checkCommand.ExecuteScalarAsync();

            if (exists == 0)
            {
                using var insertCommand = new SQLiteCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@name", nationalizeData.Name);
                insertCommand.Parameters.AddWithValue("@countryId", country.Country_Id);
                insertCommand.Parameters.AddWithValue("@probability", country.Probability);
                await insertCommand.ExecuteNonQueryAsync();
                Console.WriteLine($"Name: {nationalizeData.Name}, Country: {country.Country_Id}, Probability: {country.Probability}");
            }
            else
            {
                Console.WriteLine($"Record for Name: {nationalizeData.Name} and Country: {country.Country_Id} already exists.");
            }
        }

        Console.WriteLine("Data saved to database.");
    }


    private static void CreateDatabase(SQLiteConnection connection)
    {
        var query = @"
        CREATE TABLE IF NOT EXISTS Nationalities (
            Name TEXT,
            Country_Id TEXT,
            Probability REAL,
            PRIMARY KEY (Name, Country_Id)
        )";
        using var command = new SQLiteCommand(query, connection);
        command.ExecuteNonQuery();
    }


    private static void ReadNationalities(SQLiteConnection connection)
    {
        var query = "SELECT * FROM Nationalities";
        using var command = new SQLiteCommand(query, connection);
        using var reader = command.ExecuteReader();
        if (!reader.HasRows)
        {
            Console.WriteLine("Table is empty.");
            return;
        }
        while (reader.Read())
        {
            Console.WriteLine($"Country: {reader["Country_Id"]}, Name: {reader["Name"]}, Probability: {reader["Probability"]}");
        }
    }

    private static void ClearNationalities(SQLiteConnection connection)
    {
        var query = "DELETE FROM Nationalities";
        using var command = new SQLiteCommand(query, connection);
        command.ExecuteNonQuery();
        Console.WriteLine("Nationalities table cleared.");
    }
}
