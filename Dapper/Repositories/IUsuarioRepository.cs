using eCommerce.Models;

namespace eCommerce.Repositories;

public interface IUsuarioRepository
{
    public List<Usuario> Get();
    public Usuario Get(int id);
    public void Create(Usuario usuario);
    public void Update(Usuario usuario);
    public void Delete(int id);
}
