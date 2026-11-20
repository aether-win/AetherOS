@{
    Name        = 'Applies Aether'' Network Settings'
    Description = 'Applies Aether'' optimised network settings. Disables vendor NIC power-saving features (green Ethernet, DMA coalescing and similar) for consistent latency, which slightly increases idle power draw - relevant on laptops.'
    Toggle      = @(
        @{ Name = 'DefaultAtlasNetwork'; State = 'Enable' }
    )
}
