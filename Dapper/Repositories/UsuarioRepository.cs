using eCommerce.Models;
using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace eCommerce.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly IConfiguration _configuration;
    private readonly IDbConnection _connection;
    private string _connectionString;
    public UsuarioRepository(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetSection("ConnectionStrings")["Dapper"];
        _connection = new SqlConnection(_connectionString);
    }

    // ADO.NET > Dapper: Micro-ORM (MER <-> POO)
    // Dapper trabalha com extensões para o IDbConnection
    public List<Usuario> Get()
    {
        // o Dapper vai pegar cada coluna da tabela e colocar na propriedade equivalente
        // os nomes das colunas (MER) e das propriedades (POO) devem coincidir
        // é possível aplicar ALIAS ao SQL para que dê match
        // ou usar uma extensão do Dapper que faz o mapeamento
        return _connection.Query<Usuario>("SELECT * FROM Usuarios").ToList();
    }

    public Usuario Get(int id)
    {
        // tanto o QueryFirst quanto QuerySingle lançam exeçeção caso não retorne linhas
        return _connection.QuerySingleOrDefault<Usuario>("SELECT * FROM USUARIOS WHERE Id = @Id", new { Id = id });
    }

    public void Create(Usuario usuario)
    {
        string sql = "INSERT INTO Usuarios (Nome, Email, Sexo, RG, CPF, NomeMae, SituacaoCadastro, DataCadastro) "
            + "VALUES (@Nome, @Email, @Sexo, @RG, @CPF, @NomeMae, @SituacaoCadastro, @DataCadastro); "
            + "SELECT CAST(SCOPE_IDENTITY() AS INT);";
        // retorna o ID do usuário que acabou de ser inserido no mesmo escopo de execução

        // seria possível mapear utilizando um objeto anônimo
        // _connection.Query(sql, new { Nome = "Jose", Email = "jose@email.com" });

        // quando for ter algum retorno você usa o Query<>()
        // caso contrário, usa o Execute()
        usuario.Id = _connection.Query<int>(sql, usuario).Single();
    }

    public void Update(Usuario usuario)
    {
        string sql = "UPDATE Usuarios SET Nome = @Nome, Email = @Email, Sexo = @Sexo, RG = @RG, CPF = @CPF, "
            + "NomeMae = @NomeMae, SituacaoCadastro = @SituacaoCadastro, DataCadastro = @DataCadastro "
            + "WHERE Id = @Id";

        _connection.Execute(sql, usuario);
    }

    public void Delete(int id)
    {
        string sql = "DELETE FROM Usuarios WHERE Id = @Id";

        _connection.Execute(sql, new { Id = id });
    }




}
