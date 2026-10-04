using HomeServices.Domain.Common;

namespace HomeServices.Domain.Tests.Common;

public class EntityTests
{
    private sealed class Partner : Entity
    {
        public Partner() { }

        public Partner(Guid id) => Id = id;

        public void Approve() => RaiseDomainEvent(new PartnerApproved(Id));
    }

    private sealed class Order : Entity
    {
        public Order(Guid id) => Id = id;
    }

    private sealed record PartnerApproved(Guid PartnerId) : IDomainEvent;

    [Fact]
    public void New_entities_get_a_unique_time_ordered_id()
    {
        var first = new Partner();
        var second = new Partner();

        first.Id.ShouldNotBe(Guid.Empty);
        first.Id.Version.ShouldBe(7);
        second.Id.ShouldNotBe(first.Id);
    }

    [Fact]
    public void Entities_of_the_same_type_with_the_same_id_are_equal()
    {
        var id = Guid.NewGuid();

        new Partner(id).Equals(new Partner(id)).ShouldBeTrue();
        new Partner(id).GetHashCode().ShouldBe(new Partner(id).GetHashCode());
    }

    [Fact]
    public void Entities_with_different_ids_are_not_equal()
    {
        new Partner().Equals(new Partner()).ShouldBeFalse();
    }

    [Fact]
    public void Entities_of_different_types_are_not_equal_even_with_the_same_id()
    {
        var id = Guid.NewGuid();

        new Partner(id).Equals(new Order(id)).ShouldBeFalse();
    }

    [Fact]
    public void An_entity_is_not_equal_to_null_or_another_kind_of_object()
    {
        var partner = new Partner();

        partner.Equals(null).ShouldBeFalse();
        partner.Equals("not an entity").ShouldBeFalse();
    }

    [Fact]
    public void Domain_events_are_collected_until_cleared()
    {
        var partner = new Partner();

        partner.Approve();

        partner.DomainEvents.ShouldHaveSingleItem().ShouldBe(new PartnerApproved(partner.Id));

        partner.ClearDomainEvents();

        partner.DomainEvents.ShouldBeEmpty();
    }
}
