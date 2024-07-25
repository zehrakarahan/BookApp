﻿using Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace Application.Repositories;

public interface IRoleRepository
{
    public Task<IdentityResult> CreateRole(string name);
    public IList<string> GetList();
}
