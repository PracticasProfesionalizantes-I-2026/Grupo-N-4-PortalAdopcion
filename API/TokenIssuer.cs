using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PortalAdopcion.BusinessLogic.Mapping;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.Shared.DTOs;

public class TokenIssuer(string key)
{
    public SesionResponseDTO Issue(Usuario u)
    {
        var expires = DateTime.UtcNow.AddHours(2);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new(ClaimTypes.Name, u.Nombre),
            new(ClaimTypes.Role, u.Rol),
            new("version", u.Version.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        if (u.AdoptanteId.HasValue)
            claims.Add(new("adoptanteId", u.AdoptanteId.Value.ToString()));
        var jwt = new JwtSecurityToken("PortalAdopcion", "PortalAdopcion", claims, expires: expires, signingCredentials: new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        return new SesionResponseDTO(new JwtSecurityTokenHandler().WriteToken(jwt), expires, u.ToDto());
    }
}
