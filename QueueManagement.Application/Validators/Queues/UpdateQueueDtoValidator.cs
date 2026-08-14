using FluentValidation;
using QueueManagement.Application.DTOs.Queues;

namespace QueueManagement.Application.Validators.Queues;

public class UpdateQueueDtoValidator : AbstractValidator<UpdateQueueDto>
{
    public UpdateQueueDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Queue name is required")
            .MinimumLength(3).WithMessage("Queue name must be at least 3 characters")
            .MaximumLength(100).WithMessage("Queue name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.EstimatedTimePerItem)
            .InclusiveBetween(1, 120).WithMessage("Estimated time must be between 1 and 120 minutes");
    }
}