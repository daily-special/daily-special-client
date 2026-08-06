using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class GuestProfileScreen : MonoBehaviour
{
    [SerializeField] private Text nameLabel;
    [SerializeField] private Text titleLabel;
    [SerializeField] private Text bioLabel;
    [SerializeField] private Text statusLabel;

    public void Configure(
        Text configuredNameLabel,
        Text configuredTitleLabel,
        Text configuredBioLabel,
        Text configuredStatusLabel)
    {
        nameLabel = configuredNameLabel;
        titleLabel = configuredTitleLabel;
        bioLabel = configuredBioLabel;
        statusLabel = configuredStatusLabel;
    }

    private void Awake()
    {
        try
        {
            ContentPackage<GuestRecord> guests = ContentLoader.LoadGuests();
            GuestRecord guest = guests.items[0];

            nameLabel.text = guest.name;
            titleLabel.text = guest.title;
            bioLabel.text = guest.bio;
            statusLabel.text = "목 JSON을 읽었습니다";
        }
        catch (Exception exception)
        {
            nameLabel.text = "콘텐츠를 읽지 못했습니다";
            titleLabel.text = "";
            bioLabel.text = exception.Message;
            statusLabel.text = "오류";
            Debug.LogException(exception);
        }
    }
}
