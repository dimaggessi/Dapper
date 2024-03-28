using eCommerce.Models;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using eCommerce.Controllers;

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
        /* no Dapper cada coluna da tabela vai ser colocada na propriedade equivalente;
        os nomes das colunas (MER) e das propriedades (POO) devem coincidir;
        é possível aplicar ALIAS ao SQL para que dê match
        ou usar uma extensão do Dapper que faz o mapeamento*/

        string sql = "SELECT U.*, C.*, EE.*, D.* FROM Usuarios AS U "
            + "LEFT JOIN Contatos AS C ON C.UsuarioId = U.Id "
            + "LEFT JOIN EnderecosEntrega AS EE ON EE.UsuarioId = U.Id "
            + "LEFT JOIN UsuariosDepartamentos AS UD ON UD.UsuarioId = U.Id "
            + "LEFT JOIN Departamentos AS D ON D.Id = UD.DepartamentoId;";

        // Lista que será retornada
        List<Usuario> usuarios = new();

        // Delegate para encapsular a função map (melhorar a legibilidade)
        Func<Usuario, Contato, EnderecoEntrega, Departamento, Usuario> map = (usuario, contato, enderecoEntrega, departamento) => 
        {
            // verificação do usuário
            if(usuarios.SingleOrDefault(u => u.Id == usuario.Id) is null)
            {
                usuario.EnderecosEntrega = new List<EnderecoEntrega>();
                usuario.Departamentos = new List<Departamento>();

                // adiciona na Lista "usuarios" para evitar duplicidades
                usuario.Contato = contato;
                usuarios.Add(usuario);
            }
            else
            {
                // sobrescreve a referência da variável "usuario" recebida na função map;
                // dessa maneira vai receber um endereço novo a cada execução de "leitura de linha no DB",
                // porém mantendo o mesmo usuário e contato
                usuario = usuarios.SingleOrDefault(u => u.Id == usuario.Id);
            }

            // adiciona endereço de entrega a cada iteração
            // verifica se o endereço já foi mapeado, para evitar repetições (existe um na lista com mesmo Id)
            if (enderecoEntrega is not null && usuario.EnderecosEntrega.SingleOrDefault(e => e.Id == enderecoEntrega.Id) is null)
            {
                usuario.EnderecosEntrega.Add(enderecoEntrega);
            }

            // adiciona um departamento a cada iteração
            // verifica se o departamento já foi mapeado (existe um na lista com mesmo Id)
            if (departamento is not null && usuario.Departamentos.SingleOrDefault(d => d.Id == departamento.Id) is null)
            {
                usuario.Departamentos.Add(departamento);
            }

            return usuario;
        };

        // mapeia o usuario, contatos e endereços para a Lista de usuarios através da função map
        _connection.Query<Usuario, Contato, EnderecoEntrega, Departamento, Usuario>(sql, map);

        // a lista mapeada é retornada
        return usuarios;
    }

    public Usuario Get(int id)
    {
        /*        
        // tanto o QueryFirst quanto QuerySingle lançam exeçeção caso não retorne linhas
        // o método Query<T>() aceita até 7 JOINS em tabelas fortemente tipadas

        // TFirst, TSecond, (...), TResult
        return _connection.Query<Usuario, Contato, Usuario>(
            "SELECT * FROM Usuarios AS U LEFT JOIN Contatos AS C ON C.UsuarioId = U.Id WHERE U.Id = @Id",

            // essa função map será executada para cada linha que a query retornar
            // mapeia as colunas de usuario e contato para as variáveis de entrada da função
            (usuario, contato) =>
            {
                usuario.Contato = contato;
                return usuario;
            },

            new { Id = id }).SingleOrDefault();*/


        string sql = "SELECT U.*, C.*, EE.*, D.* FROM Usuarios AS U "
            + "LEFT JOIN Contatos AS C ON C.UsuarioId = U.Id "
            + "LEFT JOIN EnderecosEntrega AS EE ON EE.UsuarioId = U.Id "
            + "LEFT JOIN UsuariosDepartamentos AS UD ON UD.UsuarioId = U.Id "
            + "LEFT JOIN Departamentos AS D ON D.Id = UD.DepartamentoId "
            + "WHERE U.Id = @Id;";

        List<Usuario> usuarios = new();

        Func<Usuario, Contato, EnderecoEntrega, Departamento, Usuario> map = (usuario, contato, enderecoEntrega, departamento) =>
        {
            if (usuarios.SingleOrDefault(u => u.Id == usuario.Id) is null)
            {
                usuario.EnderecosEntrega = new List<EnderecoEntrega>();
                usuario.Departamentos = new List<Departamento>();

                usuario.Contato = contato;
                usuarios.Add(usuario);
            }
            else
            {
                usuario = usuarios.SingleOrDefault(u => u.Id == usuario.Id);
            }


            if (enderecoEntrega is not null && usuario.EnderecosEntrega.SingleOrDefault(e => e.Id == enderecoEntrega.Id) is null)
            {
                usuario.EnderecosEntrega.Add(enderecoEntrega);
            }

            if (departamento is not null && usuario.Departamentos.SingleOrDefault(d => d.Id == departamento.Id) is null)
            {
                usuario.Departamentos.Add(departamento);
            }

            return usuario;
        };

        _connection.Query<Usuario, Contato, EnderecoEntrega, Departamento, Usuario>(sql, map, new {Id = id});

        return usuarios.SingleOrDefault();
    }

    public void Create(Usuario usuario)
    {
        // Transaction
        _connection.Open();
        var transaction = _connection.BeginTransaction(); // a transaction será atribuída às consultas

        try
        {
            string sql = "INSERT INTO Usuarios (Nome, Email, Sexo, RG, CPF, NomeMae, SituacaoCadastro, DataCadastro) "
        + "VALUES (@Nome, @Email, @Sexo, @RG, @CPF, @NomeMae, @SituacaoCadastro, @DataCadastro); "
        + "SELECT CAST(SCOPE_IDENTITY() AS INT);";
            // retorna o ID do usuário que acabou de ser inserido no mesmo escopo de execução

            // seria possível mapear utilizando um objeto anônimo
            // _connection.Query(sql, new { Nome = "Jose", Email = "jose@email.com" });

            // quando for ter algum retorno você usa o Query<>()
            // caso contrário, usa o Execute()
            usuario.Id = _connection.Query<int>(sql, usuario, transaction).Single();

            // Inserção Contato:
            if (usuario.Contato is not null)
            {
                usuario.Contato.UsuarioId = usuario.Id;

                string sqlContato = "INSERT INTO Contatos (UsuarioId, Telefone, Celular) "
                    + "VALUES (@UsuarioId, @Telefone, @Celular); "
                    + "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                usuario.Contato.Id = _connection.Query<int>(sqlContato, usuario.Contato, transaction).Single();
            }

            if (usuario.EnderecosEntrega is not null && usuario.EnderecosEntrega.Count > 0)
            {
                foreach(var enderecoEntrega in usuario.EnderecosEntrega)
                {
                    // recebe o Id do usuário que foi inserido
                    enderecoEntrega.UsuarioId = usuario.Id;

                    string sqlEndereco = "INSERT INTO EnderecosEntrega "
                    + "(UsuarioId, NomeEndereco, CEP, Estado, Cidade, Bairro, Endereco, Numero, Complemento) "
                    + "VALUES (@UsuarioId, @NomeEndereco, @CEP, @Estado, @Cidade, @Bairro, @Endereco, @Numero, @Complemento); "
                    + "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    enderecoEntrega.Id = _connection.Query<int>(sqlEndereco, enderecoEntrega, transaction).Single();
                }
            }

            if (usuario.Departamentos is not null && usuario.Departamentos.Count > 0)
            {
                foreach (var departamento in usuario.Departamentos)
                {
                    string sqlUsuariosDepartamentos = "INSERT INTO UsuariosDepartamentos "
                    + "(UsuarioId, DepartamentoId) "
                    + "VALUES (@UsuarioId, @DepartamentoId);";

                    _connection.Execute(sqlUsuariosDepartamentos, new { UsuarioId = usuario.Id, DepartamentoId = departamento.Id }, transaction);
                }
            }

            // Persiste a informação no DB
            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
        finally
        {
            _connection.Close();
        }
    }

    public void Update(Usuario usuario)
    {
        _connection.Open();
        var transaction = _connection.BeginTransaction();

        try
        {
            string sqlUsuario = "UPDATE Usuarios SET Nome = @Nome, Email = @Email, Sexo = @Sexo, RG = @RG, CPF = @CPF, " 
                + "NomeMae = @NomeMae, SituacaoCadastro = @SituacaoCadastro, DataCadastro = @DataCadastro "
                + "WHERE Id = @Id";

            _connection.Execute(sqlUsuario, usuario, transaction);

            if (usuario.Contato is not null)
            {
                string sqlContato = "UPDATE Contatos SET Telefone = @Telefone, Celular = @Celular WHERE Id = @Id;";

                _connection.Execute(sqlContato, usuario.Contato, transaction);
            }

            if (usuario.EnderecosEntrega is not null && usuario.EnderecosEntrega.Count > 0)
            {
                string sqlDeleteEnderecos = "DELETE FROM EnderecosEntrega WHERE UsuarioId = @Id";
                _connection.Execute(sqlDeleteEnderecos, usuario, transaction);

                foreach (var enderecoEntrega in usuario.EnderecosEntrega)
                {
                    enderecoEntrega.UsuarioId = usuario.Id;

                    string sqlEndereco = "INSERT INTO EnderecosEntrega "
                    + "(UsuarioId, NomeEndereco, CEP, Estado, Cidade, Bairro, Endereco, Numero, Complemento) "
                    + "VALUES (@UsuarioId, @NomeEndereco, @CEP, @Estado, @Cidade, @Bairro, @Endereco, @Numero, @Complemento); "
                    + "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    enderecoEntrega.Id = _connection.Query<int>(sqlEndereco, enderecoEntrega, transaction).Single();
                }
            }

            if (usuario.Departamentos is not null && usuario.Departamentos.Count > 0)
            {
                string sqlDeleteDepartamentos = "DELETE FROM UsuariosDepartamentos WHERE UsuarioId = @Id";
                _connection.Execute(sqlDeleteDepartamentos, usuario, transaction);

                foreach (var departamento in usuario.Departamentos)
                {
                    string sqlUsuariosDepartamentos = "INSERT INTO UsuariosDepartamentos "
                    + "(UsuarioId, DepartamentoId) "
                    + "VALUES (@UsuarioId, @DepartamentoId);";

                    _connection.Execute(sqlUsuariosDepartamentos, new { UsuarioId = usuario.Id, DepartamentoId = departamento.Id }, transaction);
                }
            }

            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
        finally
        {
            _connection.Close();
        }
    }

    public void Delete(int id)
    {
        // Constraint - ForeignKey - ON DELETE CASCADE
        // uma vez excluído um usuário, os demais registros em outras tabelas serão excluídos
        // em cascata
        string sql = "DELETE FROM Usuarios WHERE Id = @Id";

        _connection.Execute(sql, new { Id = id });
    }
}
