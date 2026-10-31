//using FluentValidation;
//using POS.Contract.Dtos.CategoryDtos;

//namespace BackOffice.Blazor.Validations;

//public class CreateCategoryValidator : AbstractValidator<CreateCategoryDto>
//{
//    public CreateCategoryValidator(IStringLocalizer<Resource> Localizer)
//    {
//        RuleFor(x => x.ArabicName)
//            .NotEmpty().WithMessage(Localizer["ArabicNameIsRequired"]);
//                //.Must(ValidationHelpers.BeArabic).WithMessage("Arabic name must be arabic");

//        //RuleFor(x => x.EnglishName)
//        //    .NotEmpty().WithMessage("English name is required");

//        RuleFor(x => x.ItemsFont)
//            .NotEmpty().WithMessage(Localizer["ItemsFontIsRequired"]);
//    }
//}
