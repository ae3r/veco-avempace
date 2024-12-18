global using Application.Common.Interfaces;
global using Application.Common.Interfaces.Caching;
global using Application.Common.Mappings;
global using Application.Common.Models;
global using Application.Features.Clients.Caching;
global using Application.Features.Clients.Commands.AddEdit;
global using Application.Features.Clients.DTOs;
global using AutoMapper;
global using Domain.Entities;
global using FluentValidation;
global using MediatR;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.Extensions.Primitives;
global using LazyCache;
global using Microsoft.Extensions.Logging;