using UnityEngine;
using DreamGames.Board.Items;
using DreamGames.Board.Systems;
using DreamGames.Board.Visuals;
using DreamGames.Core;
using DreamGames.Data;
using DreamGames.Gameplay;
using DreamGames.UI;

namespace DreamGames.Board.Systems
{
public class GravitySystem
{
    private readonly GridSystem _grid;
    private readonly BoardGeometry _geometry;
    private readonly BoardAnimationConfig _animationConfig;

    public GravitySystem(GridSystem grid, float cellSize, BoardAnimationConfig animationConfig = null)
        : this(grid, new BoardGeometry(null, cellSize), animationConfig)
    {
    }

    public GravitySystem(GridSystem grid, BoardGeometry geometry, BoardAnimationConfig animationConfig = null)
    {
        _grid = grid;
        _geometry = geometry ?? new BoardGeometry(null, 1f);
        _animationConfig = animationConfig ?? BoardAnimationConfig.Default;
    }

    // Animated: compacts each column in one pass, returns max animation time across all columns.
    // Items in the same column stagger bottom-first; lower item starts immediately, each one above
    // waits GravityStepDelay longer, producing a waterfall cascade.
    public float ApplyGravityAndAnimate()
    {
        float maxTime = 0f;
        for (int x = 0; x < _grid.Width; x++)
        {
            float colTime = ResolveColumnAnimated(x);
            if (colTime > maxTime) maxTime = colTime;
        }
        return maxTime;
    }

    // Immediate (no animation): used by ResolveImmediate for instant board state correction.
    public bool ApplyGravity()
    {
        bool anyMoved = false;
        for (int x = 0; x < _grid.Width; x++)
        {
            anyMoved |= ResolveColumnImmediate(x);
        }
        return anyMoved;
    }

    private float ResolveColumnAnimated(int x)
    {
        int writeY = 0;
        int staggerIndex = 0;
        float maxTime = 0f;

        for (int y = 0; y < _grid.Height; y++)
        {
            if (!_grid.CanHoldItem(x, y))
            {
                writeY = y + 1;
                staggerIndex = 0;
                continue;
            }

            var item = _grid.GetItem(x, y);
            if (item == null) continue;

            if (!(item is IFallable))
            {
                // Non-fallable obstacle (stone, box, vase): treat as floor, reset stagger group.
                writeY = y + 1;
                staggerIndex = 0;
                continue;
            }

            if (y > writeY)
            {
                _grid.SetItem(x, y, null);
                _grid.SetItem(x, writeY, item);

                int fallDistance = y - writeY;
                float invSpeed = 1f / GameDebug.SpeedMultiplier;
                float fallDuration = _animationConfig.GetFallDuration(fallDistance) * invSpeed;
                float fallDelay = _animationConfig.GetCascadeDelay(staggerIndex) * invSpeed;
                float bounceDuration = _animationConfig.LandingBounceDuration * invSpeed;

                if (item is AbstractBoardItem abi)
                {
                    Vector3 targetPos = _geometry.CellToLocalPosition(x, writeY);
                    abi.SetPosition(x, writeY);
                    abi.FallToPositionDelayed(
                        targetPos,
                        fallDelay,
                        fallDuration,
                        _animationConfig.LandingBounceHeight,
                        bounceDuration);
                }
                else
                {
                    GameObject go = item.GetGameObject();
                    if (go != null)
                        go.transform.localPosition = _geometry.CellToLocalPosition(x, writeY);
                }

                float totalTime = fallDelay + fallDuration + bounceDuration;
                if (totalTime > maxTime) maxTime = totalTime;

                staggerIndex++;
            }
            else
            {
                // Item already in final position; reset stagger so the next falling group starts fresh.
                staggerIndex = 0;
            }

            writeY++;
        }

        return maxTime;
    }

    private bool ResolveColumnImmediate(int x)
    {
        int writeY = 0;
        bool moved = false;

        for (int y = 0; y < _grid.Height; y++)
        {
            if (!_grid.CanHoldItem(x, y))
            {
                writeY = y + 1;
                continue;
            }

            var item = _grid.GetItem(x, y);
            if (item == null) continue;

            if (!(item is IFallable))
            {
                writeY = y + 1;
                continue;
            }

            if (y > writeY)
            {
                _grid.SetItem(x, y, null);
                _grid.SetItem(x, writeY, item);

                Vector3 targetPos = _geometry.CellToLocalPosition(x, writeY);
                if (item is AbstractBoardItem abi)
                {
                    abi.SetPosition(x, writeY);
                    abi.SnapToPosition(targetPos);
                }
                else
                {
                    GameObject go = item.GetGameObject();
                    if (go != null) go.transform.localPosition = targetPos;
                }

                moved = true;
            }

            writeY++;
        }

        return moved;
    }
}
}
