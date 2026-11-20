@{
    Name        = 'Apply Privacy Toggle Defaults'
    Description = 'Disables recent-item tracking and web search (Aether: Vincular teléfono se queda activo e instalado) through their toggle definitions, recording each applied choice for upgrade and user replay.'
    Toggle      = @(
        @{ Name = 'RecentItems'; State = 'Disable' }
        @{ Name = 'WebSearch'; State = 'Disable' }
    )
}
