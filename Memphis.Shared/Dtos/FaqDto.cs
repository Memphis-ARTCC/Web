﻿namespace Memphis.Shared.Dtos;

public class FaqDto
{
    public required string Question { get; set; }
    public required string Answer { get; set; }
    public int Order { get; set; }
}