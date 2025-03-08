/*
 * VFXAnimator.cs
 * 
 * Author: Aron Ireal Lewis Pence
 * License: Creative Commons Attribution 4.0 International (CC BY 4.0)
 * 
 * You are free to:
 * - Share — copy and redistribute the material in any medium or format.
 * - Adapt — remix, transform, and build upon the material for any purpose, even commercially.
 * 
 * Attribution required. Full license: https://creativecommons.org/licenses/by/4.0/
 * 
 * Description:
 * This script manages the fading and visibility of multiple CanvasGroups in Unity.
 * It allows you to start with the UI hidden or visible, fade in/out smoothly,
 * and optionally perform a flicker effect after fading in.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles fading in/out sequences for CanvasGroups and Images, with optional "glitch" flicker effects.
/// Includes an AnimationCurve for easing, timed delays, and a testVisibility flag for runtime toggling.
/// </summary>
public class VFXAnimator : MonoBehaviour
{
    #region Inspector Fields

    [Header("Elements to Animate")]
    public List<CanvasGroup> canvasGroups = new List<CanvasGroup>();
    public List<Image> images = new List<Image>();

    [Header("Timing Settings")]
    public float startingDelay = 0f;
    public float exitDelay = 0f;
    public float delayBetweenElements = 0.1f;
    public float fadeDuration = 0.5f;

    [Header("Fade Settings")]
    public bool fadeOutAllAtOnce = false;

    [Header("Easing")]
    [Tooltip("Control the fade-in/fade-out curve. X=normalized time (0..1), Y=fade interpolation (0..1).")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Glitch Effect")]
    public bool enableGlitchEffect = false;
    public float glitchDuration = 0.1f;
    [Range(1, 5)] public int glitchRepeats = 1;
    public bool randomGlitchOrder = false;
    [Tooltip("Min alpha for flicker")]
    public float glitchAlphaMin = 0.1f;
    [Tooltip("Max alpha for flicker")]
    public float glitchAlphaMax = 0.2f;

    [Header("Debug / Testing")]
    [SerializeField, Tooltip("Toggle at runtime to force fade in or out")]
    private bool testVisibility = false;

    #endregion

    #region Private Fields

    private bool lastTestVisibility;
    private Coroutine fadeRoutine;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Unity's Start method, called before the first frame update.
    /// Hides all elements by default and checks testVisibility to decide whether to fade in.
    /// </summary>
    private void Start()
    {
        // Start hidden
        SetAlpha(0f);
        SetFill(0f);

        // Track initial testVisibility
        lastTestVisibility = testVisibility;
        if (testVisibility)
            PlayFadeIn();
    }

    /// <summary>
    /// Unity's Update method, called once per frame.
    /// If testVisibility changes, triggers fade in/out accordingly.
    /// </summary>
    private void Update()
    {
        if (testVisibility != lastTestVisibility)
        {
            lastTestVisibility = testVisibility;
            if (testVisibility) PlayFadeIn();
            else PlayFadeOut();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Public method to initiate a fade-in sequence for all elements.
    /// </summary>
    public void PlayFadeIn()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeSequence(true));
    }

    /// <summary>
    /// Public method to initiate a fade-out sequence for all elements.
    /// Accepts an optional callback to invoke upon completion.
    /// </summary>
    /// <param name="onComplete">Callback action that runs after fade-out is finished</param>
    public void PlayFadeOut(Action onComplete = null)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeSequence(false, onComplete));
    }

    #endregion

    #region Coroutines

    /// <summary>
    /// Handles the overall fade in/out process, including optional glitch effects.
    /// Waits startingDelay or exitDelay before beginning, then applies fade logic to each element.
    /// </summary>
    /// <param name="fadeIn">True for fade-in, false for fade-out</param>
    /// <param name="onComplete">Optional callback after finishing</param>
    private IEnumerator FadeSequence(bool fadeIn, Action onComplete = null)
    {
        yield return new WaitForSeconds(startingDelay);

        if (fadeIn)
        {
            int maxCount = Mathf.Max(canvasGroups.Count, images.Count);

            for (int i = 0; i < maxCount; i++)
            {
                // Fade in each new element
                if (i < canvasGroups.Count)
                    StartCoroutine(FadeCanvasGroup(canvasGroups[i], fadeDuration, 1f));
                if (i < images.Count)
                    StartCoroutine(FillImage(images[i], fadeDuration, 1f));

                // Optionally glitch the previously finished elements
                if (enableGlitchEffect)
                {
                    yield return StartCoroutine(GlitchPreviousElements(i));
                }

                yield return new WaitForSeconds(delayBetweenElements);
            }

            // Ensure everything ends at full alpha/fill
            SetAlpha(1f);
            SetFill(1f);
        }
        else
        {
            // Fade Out
            yield return new WaitForSeconds(exitDelay);

            if (fadeOutAllAtOnce)
            {
                foreach (var group in canvasGroups)
                    StartCoroutine(FadeCanvasGroup(group, fadeDuration, 0f));
                foreach (var img in images)
                    StartCoroutine(FillImage(img, fadeDuration, 0f));

                yield return new WaitForSeconds(fadeDuration);
            }
            else
            {
                int maxCount = Mathf.Max(canvasGroups.Count, images.Count);
                for (int i = 0; i < maxCount; i++)
                {
                    if (i < canvasGroups.Count)
                        StartCoroutine(FadeCanvasGroup(canvasGroups[i], fadeDuration, 0f));
                    if (i < images.Count)
                        StartCoroutine(FillImage(images[i], fadeDuration, 0f));

                    yield return new WaitForSeconds(delayBetweenElements);
                }
            }
        }

        onComplete?.Invoke();
    }

    /// <summary>
    /// Coroutine that linearly interpolates a CanvasGroup's alpha from current to target,
    /// using the fadeCurve for easing.
    /// Also handles interactability if alpha is above 0.5.
    /// </summary>
    /// <param name="group">CanvasGroup to fade</param>
    /// <param name="duration">Duration of the fade</param>
    /// <param name="targetAlpha">Target alpha value [0..1]</param>
    private IEnumerator FadeCanvasGroup(CanvasGroup group, float duration, float targetAlpha)
    {
        if (group == null) yield break;

        float startAlpha = group.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            float curveValue = fadeCurve.Evaluate(t);
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, curveValue);

            yield return null;
        }

        group.alpha = targetAlpha;
        group.interactable = group.blocksRaycasts = (targetAlpha > 0.5f);
    }

    /// <summary>
    /// Coroutine that transitions an Image's fillAmount from current to target,
    /// using the fadeCurve for easing.
    /// </summary>
    /// <param name="image">Image to fill</param>
    /// <param name="duration">Duration of the fill</param>
    /// <param name="targetFill">Target fillAmount [0..1]</param>
    private IEnumerator FillImage(Image image, float duration, float targetFill)
    {
        if (image == null) yield break;

        float startFill = image.fillAmount;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            float curveValue = fadeCurve.Evaluate(t);
            image.fillAmount = Mathf.Lerp(startFill, targetFill, curveValue);

            yield return null;
        }

        image.fillAmount = targetFill;
    }

    /// <summary>
    /// Coroutine that applies a flicker "glitch" effect to all elements before the current index.
    /// Repeats flicker for glitchRepeats times, optionally in random order.
    /// </summary>
    /// <param name="currentIndex">Index of the newly animated element</param>
    private IEnumerator GlitchPreviousElements(int currentIndex)
    {
        if (currentIndex == 0) yield break;

        List<int> indices = new List<int>();
        for (int j = 0; j < currentIndex; j++)
            indices.Add(j);

        if (randomGlitchOrder)
            Shuffle(indices);

        foreach (int idx in indices)
        {
            for (int r = 0; r < glitchRepeats; r++)
            {
                // Flicker CanvasGroup
                if (idx < canvasGroups.Count && canvasGroups[idx] != null)
                {
                    CanvasGroup grp = canvasGroups[idx];
                    float originalAlpha = grp.alpha;
                    float randomAlpha = UnityEngine.Random.Range(glitchAlphaMin, glitchAlphaMax);

                    grp.alpha = randomAlpha;
                    yield return new WaitForSeconds(glitchDuration);

                    grp.alpha = originalAlpha;
                }

                // Flicker Image
                if (idx < images.Count && images[idx] != null)
                {
                    Image img = images[idx];
                    float originalFill = img.fillAmount;
                    float randomFill = Mathf.Clamp01(
                        UnityEngine.Random.Range(glitchAlphaMin, glitchAlphaMax)
                    );

                    img.fillAmount = randomFill;
                    yield return new WaitForSeconds(glitchDuration);

                    img.fillAmount = originalFill;
                }
            }
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Shuffles the given list in-place using Fisher-Yates algorithm.
    /// </summary>
    private void Shuffle<T>(IList<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    /// <summary>
    /// Instantly sets alpha on all CanvasGroups to a specific value.
    /// Also handles interactability for alpha > 0.5f.
    /// </summary>
    private void SetAlpha(float value)
    {
        foreach (var group in canvasGroups)
        {
            if (group != null)
            {
                group.alpha = value;
                group.interactable = group.blocksRaycasts = (value > 0.5f);
            }
        }
    }

    /// <summary>
    /// Instantly sets fillAmount on all Images to a specific value.
    /// </summary>
    private void SetFill(float value)
    {
        foreach (var image in images)
        {
            if (image != null)
                image.fillAmount = value;
        }
    }

    #endregion
}
