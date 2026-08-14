using FluentValidation;
using QueueManagement.Application.DTOs.Queues;

namespace QueueManagement.Application.Validators.Queues;

public class AddQueueItemDtoValidator : AbstractValidator<AddQueueItemDto>
{
    public AddQueueItemDtoValidator()
    {
        RuleFor(x => x.ItemName)
            .NotEmpty().WithMessage("Item name is required")
            .MinimumLength(2).WithMessage("Item name must be at least 2 characters")
            .MaximumLength(200).WithMessage("Item name must not exceed 200 characters");
    }
}