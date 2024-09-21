
using System.Data.SQLite;
using Newtonsoft.Json;

public class Joke
{
    public string Type { get; set; }
    public string Setup { get; set; }
    public string Punchline { get; set; }
    public int Id { get; set; }
}

class Program
{
    private const string ConnectionString = "Data Source=jokes.db;Version=3;";

    static async Task Main(string[] args)
    {
        using var connection = new SQLiteConnection(ConnectionString);
        connection.Open();
        CreateDatabase(connection);
        string choice = "";
        while (choice != "4")
        {
            Console.WriteLine("1 - Fetch new joke\n2 - Read saved jokes\n3 - Clear saved jokes\n4 - Exit");
            choice = Console.ReadLine();
            switch (choice)
            {
                case "1": await FetchAndStoreJoke(connection); break;
                case "2": ReadJokes(connection); break;
                case "3": ClearJokes(connection); break;
            }
        }
    }

    private static async Task FetchAndStoreJoke(SQLiteConnection connection)
    {
        using var client = new HttpClient();
        var response = await client.GetStringAsync("https://official-joke-api.appspot.com/random_joke");
        var joke = JsonConvert.DeserializeObject<Joke>(response);
        await SaveJokeToDatabaseAsync(joke, connection);
    }

    private static async Task SaveJokeToDatabaseAsync(Joke joke, SQLiteConnection connection)
    {
        var query = "INSERT INTO Jokes (Type, Setup, Punchline, Id) VALUES (@type, @setup, @punchline, @id)";

        Console.WriteLine($"ID: {joke.Id}, Type: {joke.Type}, Setup: {joke.Setup}, Punchline: {joke.Punchline}");

        using var command = new SQLiteCommand(query, connection);
        command.Parameters.AddWithValue("@type", joke.Type);
        command.Parameters.AddWithValue("@setup", joke.Setup);
        command.Parameters.AddWithValue("@punchline", joke.Punchline);
        command.Parameters.AddWithValue("@id", joke.Id);

        await command.ExecuteNonQueryAsync();
        Console.WriteLine("Joke saved to database.");
    }


    private static void CreateDatabase(SQLiteConnection connection)
    {
        var query = "CREATE TABLE IF NOT EXISTS Jokes (Id INTEGER PRIMARY KEY, Type TEXT, Setup TEXT, Punchline TEXT)";
        using var command = new SQLiteCommand(query, connection);
        command.ExecuteNonQuery();
    }

    private static void ReadJokes(SQLiteConnection connection)
    {
        var query = "SELECT * FROM Jokes";
        using var command = new SQLiteCommand(query, connection);
        using var reader = command.ExecuteReader();
        if (!reader.HasRows)
        {
            Console.WriteLine("Table is empty.");
            return;
        }
        while (reader.Read())
        {
            Console.WriteLine($"ID: {reader["Id"]}, Type: {reader["Type"]}, Setup: {reader["Setup"]}, Punchline: {reader["Punchline"]}");
        }
    }

    //додаткове очищення
    private static void ClearJokes(SQLiteConnection connection)
    {
        var query = "DELETE FROM Jokes";
        using var command = new SQLiteCommand(query, connection);
        command.ExecuteNonQuery();
        Console.WriteLine("Jokes table cleared.");
    }
}
