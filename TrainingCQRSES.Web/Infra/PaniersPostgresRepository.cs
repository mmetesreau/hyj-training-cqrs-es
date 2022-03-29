using Dapper;
using Npgsql;

namespace TrainingCQRSES.Web.Infra;

public class PaniersPostgresRepository : IPaniersRepository
{
    private readonly string _connectionString;


    public PaniersPostgresRepository(string connectionString)
    {
        _connectionString = connectionString;

        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Execute(SQL.CREATE_IF_NOT_EXIST);
        }
    }

    public void Set(Guid id, PanierQuantite panierQuantite)
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Execute(SQL.UPSERT, new
            {
                Id = id,
                NombreArticles = panierQuantite.NombreArticles
            });
        }
    }

    public PanierQuantite Get(Guid id)
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            return connection.QuerySingleOrDefault<PanierQuantite>(SQL.GET_BY_ID, new
            {
                Id = id
            }) ?? new PanierQuantite {NombreArticles = 0};
        }
    }

    private class SQL
    {
        public const string TABLE_NAME = "panierquantites";
        public const string Id = "id";
        public const string NombreArticles = "nombrearticles";
        
        public const string CREATE_IF_NOT_EXIST = $"CREATE TABLE IF NOT EXISTS {TABLE_NAME} ({Id} uuid NOT NULL, {NombreArticles} integer NOT NULL DEFAULT '0', PRIMARY KEY ({Id}))";

        public const string UPSERT = $"INSERT INTO {TABLE_NAME}({Id}, {NombreArticles}) VALUES(@Id, @NombreArticles) ON CONFLICT ({Id}) DO UPDATE SET {NombreArticles}=@NombreArticles";

        public const string GET_BY_ID = $"SELECT * FROM {TABLE_NAME} WHERE {Id} = @Id";
    }
}