using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Raytha.Web.Areas.Api;

namespace Raytha.Architecture.Tests;

public class BindingProblemTests
{
    [Test]
    public void A_body_that_fails_to_bind_names_the_property_and_drops_the_required_noise()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("request", "The request field is required.");
        modelState.AddModelError(
            "$.filter[0].id",
            "The JSON value could not be converted to System.Guid. Path: $.filter[0].id | LineNumber: 0 | BytePositionInLine: 26."
        );

        var problem = BindingProblem.Create(modelState, "/raytha/api/v1/contenttypes/posts/views/x/filter");

        problem.Status.Should().Be(400);
        problem.Type.Should().NotBeNullOrEmpty();
        problem.Title.Should().NotBeNullOrEmpty();
        problem.Instance.Should().Be("/raytha/api/v1/contenttypes/posts/views/x/filter");
        problem.Detail.Should().Be("filter[0].id must be a GUID.");
        problem.Errors.Keys.Should().BeEquivalentTo("$.filter[0].id");
        problem.Errors["$.filter[0].id"].Should().Equal("filter[0].id must be a GUID.");
    }

    [Test]
    public void A_missing_body_still_reports_the_required_field()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("request", "The request field is required.");

        var problem = BindingProblem.Create(modelState, "/raytha/api/v1/x");

        problem.Detail.Should().Be("The request field is required.");
        problem.Errors.Keys.Should().BeEquivalentTo("request");
    }

    [Test]
    public void A_validation_error_on_a_named_field_is_left_alone()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Label", "The Label field is required.");

        var problem = BindingProblem.Create(modelState, "/raytha/api/v1/x");

        problem.Errors["Label"].Should().Equal("The Label field is required.");
    }
}
