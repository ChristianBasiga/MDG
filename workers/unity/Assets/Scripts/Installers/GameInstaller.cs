//Zenject is a no go now man.
// So compilation here sees it but console complains?
using Zenject;
public class GameInstaller : MonoInstaller//: MonoInstaller//: Zenject.MonoInstaller
{
    public override void InstallBindings()
    {
        base.InstallBindings();
    }

}
