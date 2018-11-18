using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour {

    public Image orbFill;
    public Image orbLip;
    public float lipOffset = 0.01f;
    private PlayerStats stats;

    void Start ()
    {
        stats = PlayerStats.Instance;
    }

    private void LateUpdate()
    {
        if (stats.getHealth() > 0 && orbFill != null && orbLip != null)
        {
            orbFill.fillAmount = stats.getHealthPercentage();
            orbLip.fillAmount = orbFill.fillAmount + lipOffset;
        }
    }
}
