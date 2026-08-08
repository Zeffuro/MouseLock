namespace MouseLock.Configuration;

public sealed partial class SystemConfiguration
{
    private void Migrate()
    {
        while (Version < CurrentVersion)
        {
            switch (Version)
            {
                case <= 0:
                    Version = 1;
                    break;
                case 1:
                    MigrateToVersion2();
                    Version = 2;
                    break;
                case 2:
                    MigrateToVersion3();
                    Version = 3;
                    break;
                case 3:
                    MigrateToVersion4();
                    Version = 4;
                    break;
                default:
                    Version = CurrentVersion;
                    break;
            }
        }
    }

    private void MigrateToVersion2()
    {
        _dtr.Enabled = true;
    }

    private void MigrateToVersion3()
    {
        _general.FirstRunIntroCompleted = true;
    }

    private void MigrateToVersion4()
    {
        // StickyReleaseEnabled was replaced by the three-way ReleaseModifierTapBehavior setting.
        _general.MigrateStickyReleaseEnabled();
    }
}
