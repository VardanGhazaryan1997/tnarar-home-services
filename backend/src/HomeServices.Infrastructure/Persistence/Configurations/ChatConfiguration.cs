using HomeServices.Domain.Chat;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");

        // One conversation per request and partner.
        builder.HasIndex(c => new { c.RequestId, c.PartnerProfileId }).IsUnique();
        // "My conversations", most recent first, for each side.
        builder.HasIndex(c => new { c.CustomerId, c.LastMessageAt });
        builder.HasIndex(c => new { c.PartnerProfileId, c.LastMessageAt });

        builder.HasOne<ServiceRequest>().WithMany().HasForeignKey(c => c.RequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PartnerProfile>().WithMany().HasForeignKey(c => c.PartnerProfileId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");
        builder.Property(m => m.SenderRole).HasConversion<string>().HasMaxLength(16);
        builder.Property(m => m.Body).HasMaxLength(Message.BodyMaxLength);

        // A conversation's messages page backwards from the newest.
        builder.HasIndex(m => new { m.ConversationId, m.SentAt });

        builder.HasOne<Conversation>().WithMany().HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(m => m.SenderUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(m => m.Attachments).WithOne().HasForeignKey(a => a.MessageId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
{
    public void Configure(EntityTypeBuilder<MessageAttachment> builder)
    {
        builder.ToTable("MessageAttachments");
        builder.HasIndex(a => new { a.MessageId, a.FileId }).IsUnique();
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(a => a.FileId).OnDelete(DeleteBehavior.Restrict);
    }
}
